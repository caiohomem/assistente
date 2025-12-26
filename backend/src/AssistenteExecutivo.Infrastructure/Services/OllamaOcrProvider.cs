using AssistenteExecutivo.Domain.Interfaces;
using AssistenteExecutivo.Domain.ValueObjects;
using AssistenteExecutivo.Infrastructure.HttpClients;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace AssistenteExecutivo.Infrastructure.Services;

/// <summary>
/// OCR Provider usando Ollama com modelo de visão (LLaVA, etc).
/// </summary>
public class OllamaOcrProvider : IOcrProvider
{
    private static readonly Regex EmailRegex = new(@"\b[A-Za-z0-9._%+-]+@[A-Za-z0-9.-]+\.[A-Z|a-z]{2,}\b", RegexOptions.IgnoreCase);
    private static readonly Regex PhoneRegex = new(@"(\+55\s?)?(\(?\d{2}\)?\s?)?(\d{4,5}[-.\s]?\d{4})");
    private static readonly Regex PhoneLikeChunkRegex = new(@"[\+0-9\(\)\-\.\s]{8,}");

    private readonly OllamaClient _ollamaClient;
    private readonly ILogger<OllamaOcrProvider> _logger;
    private readonly IOcrFieldRefinementService? _refinementService;
    private readonly string _model;
    private readonly double _temperature;
    private readonly int _maxTokens;

    public OllamaOcrProvider(
        OllamaClient ollamaClient,
        IConfiguration configuration,
        ILogger<OllamaOcrProvider> logger,
        IOcrFieldRefinementService? refinementService = null)
    {
        _ollamaClient = ollamaClient;
        _logger = logger;
        _refinementService = refinementService;
        _model = configuration["Ollama:Ocr:Model"] ?? "llava";
        _temperature = double.Parse(configuration["Ollama:Ocr:Temperature"] ?? "0.1");
        _maxTokens = int.Parse(configuration["Ollama:Ocr:MaxTokens"] ?? "500");
    }

    public async Task<OcrExtract> ExtractFieldsAsync(
        byte[] imageBytes,
        string mimeType,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var imageBase64 = Convert.ToBase64String(imageBytes);

            _logger.LogInformation(
                "Enviando imagem para OCR usando modelo {Model}, MimeType={MimeType}, Bytes={Bytes}, Temperature={Temp}, MaxTokens={MaxTokens}",
                _model,
                mimeType,
                imageBytes.Length,
                _temperature,
                _maxTokens);

            var response = await _ollamaClient.GenerateAsync(
                model: _model,
                prompt: BuildPrompt(),
                imageBase64: imageBase64,
                temperature: _temperature,
                maxTokens: _maxTokens,
                cancellationToken: cancellationToken);

            _logger.LogInformation(
                "Resposta completa do Ollama (primeiros 500 chars): {Response}",
                response.Length > 500 ? response.Substring(0, 500) + "..." : response);

            var jsonText = ExtractJsonFromResponse(response);

            if (string.IsNullOrWhiteSpace(jsonText))
            {
                _logger.LogWarning("Não foi possível extrair JSON da resposta do Ollama");
                return new OcrExtract();
            }

            var extract = ParseOcrExtract(jsonText);

            // Second pass: se o modelo falhou em transcrever o telefone no rawText, tentar extrair apenas o telefone.
            if (string.IsNullOrWhiteSpace(extract.Phone))
            {
                var phoneFromSecondPass = await TryExtractPhoneFromImageAsync(imageBase64, cancellationToken);
                if (!string.IsNullOrWhiteSpace(phoneFromSecondPass))
                {
                    var updatedScores = new Dictionary<string, decimal>(extract.ConfidenceScores)
                    {
                        ["phone"] = 0.75m
                    };

                    extract = new OcrExtract(
                        rawText: extract.RawText,
                        name: extract.Name,
                        email: extract.Email,
                        phone: phoneFromSecondPass,
                        company: extract.Company,
                        jobTitle: extract.JobTitle,
                        confidenceScores: updatedScores);
                }
            }

            // Refinamento com Qwen: usar se campos estiverem incompletos ou com baixa confiança
            if (_refinementService != null && ShouldRefineExtract(extract))
            {
                _logger.LogInformation("Aplicando refinamento de campos com Qwen");
                try
                {
                    extract = await _refinementService.RefineFieldsAsync(
                        extract.RawText ?? string.Empty,
                        extract,
                        cancellationToken);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Erro ao refinar campos com Qwen, usando extração original");
                    // Continuar com extract original se refinamento falhar
                }
            }

            _logger.LogInformation(
                "OCR extraído: Name={Name}, Email={Email}, Phone={Phone}, Company={Company}, JobTitle={JobTitle}",
                extract.Name,
                extract.Email,
                extract.Phone,
                extract.Company,
                extract.JobTitle);

            return extract;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao processar OCR com Ollama");
            throw;
        }
    }

    private static string BuildPrompt()
    {
        return @"Você é um motor de OCR.

Extraia TODO o texto visível no cartão de visita da imagem. Retorne APENAS um JSON válido com estas chaves exatas:

{""rawText"":""texto completo do cartão, linha por linha"",""name"":null,""email"":null,""phone"":null,""company"":null,""jobTitle"":null}

REGRAS:
- NUNCA invente informações. Se não conseguir ler, use null.
- rawText deve conter SOMENTE texto visível (uma linha por linha), sem comentários.
- Se preencher name/email/phone/company/jobTitle, copie EXATAMENTE do rawText.
- Retorne SOMENTE o JSON (sem markdown, sem texto adicional).";
    }

    private string ExtractJsonFromResponse(string response)
    {
        var cleaned = response.Trim();

        var markdownMatch = Regex.Match(
            cleaned,
            @"```(?:json)?\s*(\{.*?\})\s*```",
            RegexOptions.Singleline | RegexOptions.IgnoreCase);

        if (markdownMatch.Success)
        {
            cleaned = markdownMatch.Groups[1].Value.Trim();
        }

        cleaned = Regex.Replace(
            cleaned,
            @"^(?:Aqui está|Here is|JSON:|Resposta:)\s*",
            "",
            RegexOptions.IgnoreCase | RegexOptions.Multiline);

        var jsonStart = cleaned.IndexOf('{');
        var jsonEnd = cleaned.LastIndexOf('}');

        if (jsonStart >= 0 && jsonEnd > jsonStart)
        {
            cleaned = cleaned.Substring(jsonStart, jsonEnd - jsonStart + 1);
        }

        try
        {
            using var _ = JsonDocument.Parse(cleaned);
            return cleaned;
        }
        catch (JsonException)
        {
            cleaned = FixCommonJsonIssues(cleaned);

            try
            {
                using var _ = JsonDocument.Parse(cleaned);
                return cleaned;
            }
            catch
            {
                _logger.LogWarning("Não foi possível extrair JSON válido da resposta: {Response}", response);
                return cleaned;
            }
        }
    }

    private static string FixCommonJsonIssues(string json)
    {
        json = Regex.Replace(json, @"(\w+)\s*}", "$1 }");
        json = Regex.Replace(json, @",\s*,", ",");
        json = Regex.Replace(json, @":\s*([^,""}\]]+?)(\s*[,}])", @": ""$1""$2");
        json = Regex.Replace(json, @"""null""", "null", RegexOptions.IgnoreCase);
        return json;
    }

    private OcrExtract ParseOcrExtract(string jsonText)
    {
        try
        {
            using var doc = JsonDocument.Parse(jsonText);
            return ParseOcrExtract(doc.RootElement);
        }
        catch (JsonException ex)
        {
            _logger.LogWarning(ex, "Erro ao fazer parse do JSON: {JsonText}", jsonText);

            try
            {
                var fixedJson = FixCommonJsonIssues(jsonText);
                using var doc = JsonDocument.Parse(fixedJson);
                return ParseOcrExtract(doc.RootElement);
            }
            catch
            {
                return ExtractViaRegex(jsonText);
            }
        }
    }

    private OcrExtract ParseOcrExtract(JsonElement root)
    {
        var rawText = ExtractStringProperty(root, "rawText");
        if (!string.IsNullOrWhiteSpace(rawText))
        {
            _logger.LogInformation(
                "OCR rawText (primeiros 500 chars): {RawText}",
                rawText.Length > 500 ? rawText.Substring(0, 500) + "..." : rawText);
        }

        var name = ExtractStringProperty(root, "name");
        var email = ExtractStringProperty(root, "email");
        var phone = ExtractStringProperty(root, "phone");
        var company = ExtractStringProperty(root, "company");
        var jobTitle = ExtractStringProperty(root, "jobTitle");

        if (!string.IsNullOrWhiteSpace(rawText))
        {
            name = AppearsInRawText(name, rawText) ? name : null;
            company = AppearsInRawText(company, rawText) ? company : null;
            jobTitle = AppearsInRawText(jobTitle, rawText) ? jobTitle : null;

            if (email != null && (!IsValidEmail(email) || !AppearsInRawText(email, rawText)))
            {
                email = null;
            }

            if (phone != null && !PhoneAppearsInRawText(phone, rawText))
            {
                phone = null;
            }

            email ??= ExtractEmailFromText(rawText);
            phone ??= ExtractPhoneFromText(rawText);
            name ??= GuessPersonNameFromText(rawText);
            company ??= GuessCompanyFromText(rawText);
            jobTitle ??= GuessJobTitleFromText(rawText, name, company);
        }

        if (email != null && !IsValidEmail(email))
        {
            _logger.LogWarning("Email inválido extraído: {Email}", email);
            email = null;
        }

        var confidenceScores = new Dictionary<string, decimal>
        {
            { "name", name != null ? 0.8m : 0m },
            { "email", email != null ? 0.9m : 0m },
            { "phone", phone != null ? 0.8m : 0m },
            { "company", company != null ? 0.8m : 0m },
            { "jobTitle", jobTitle != null ? 0.7m : 0m }
        };

        _logger.LogInformation(
            "Dados extraídos: Name={Name}, Email={Email}, Phone={Phone}, Company={Company}, JobTitle={JobTitle}",
            name ?? "null",
            email ?? "null",
            phone ?? "null",
            company ?? "null",
            jobTitle ?? "null");

        return new OcrExtract(
            rawText: rawText,
            name: name,
            email: email,
            phone: phone,
            company: company,
            jobTitle: jobTitle,
            confidenceScores: confidenceScores);
    }

    private static string? ExtractStringProperty(JsonElement root, string propertyName)
    {
        if (!root.TryGetProperty(propertyName, out var prop))
        {
            return null;
        }

        if (prop.ValueKind == JsonValueKind.Null)
        {
            return null;
        }

        if (prop.ValueKind == JsonValueKind.String)
        {
            var value = prop.GetString()?.Trim();
            if (string.IsNullOrWhiteSpace(value) || value.Equals("null", StringComparison.OrdinalIgnoreCase))
            {
                return null;
            }

            return value;
        }

        return prop.ToString().Trim();
    }

    private OcrExtract ExtractViaRegex(string text)
    {
        var email = ExtractEmailFromText(text);
        var phone = ExtractPhoneFromText(text);
        var name = GuessPersonNameFromText(text);

        var confidenceScores = new Dictionary<string, decimal>
        {
            { "name", name != null ? 0.6m : 0m },
            { "email", email != null ? 0.9m : 0m },
            { "phone", phone != null ? 0.8m : 0m },
            { "company", 0m },
            { "jobTitle", 0m }
        };

        return new OcrExtract(
            rawText: text,
            name: name,
            email: email,
            phone: phone,
            company: null,
            jobTitle: null,
            confidenceScores: confidenceScores);
    }

    private static bool IsValidEmail(string email)
    {
        try
        {
            return new Regex(@"^[^@\s]+@[^@\s]+\.[^@\s]+$", RegexOptions.IgnoreCase).IsMatch(email);
        }
        catch
        {
            return false;
        }
    }

    private static string? ExtractEmailFromText(string? text)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return null;
        }

        var match = EmailRegex.Match(text);
        return match.Success ? match.Value.Trim() : null;
    }

    private static string? ExtractPhoneFromText(string? text)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return null;
        }

        // 1) Tentar regex direto
        var match = PhoneRegex.Match(text);
        if (match.Success)
        {
            var normalized = NormalizeBrazilPhoneForDomain(match.Value);
            if (normalized != null)
            {
                return normalized;
            }
        }

        // 2) Fallback: procurar "chunks" com cara de telefone e normalizar por dA-gitos
        var best = (string?)null;
        foreach (Match chunk in PhoneLikeChunkRegex.Matches(text))
        {
            var normalized = NormalizeBrazilPhoneForDomain(chunk.Value);
            if (normalized == null)
            {
                continue;
            }

            if (best == null || normalized.Length > best.Length)
            {
                best = normalized;
            }
        }

        return best;
    }

    private static bool AppearsInRawText(string? value, string rawText)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return false;
        }

        return rawText.IndexOf(value, StringComparison.OrdinalIgnoreCase) >= 0;
    }

    private static bool PhoneAppearsInRawText(string phone, string rawText)
    {
        var phoneDigits = DigitsOnly(phone);
        if (phoneDigits.Length < 8)
        {
            return false;
        }

        var rawDigits = DigitsOnly(rawText);
        if (rawDigits.Contains(phoneDigits, StringComparison.Ordinal))
        {
            return true;
        }

        // Se o rawText tiver cA3digo do paA-s +55, aceitar match com prefixo
        if (!phoneDigits.StartsWith("55", StringComparison.Ordinal) &&
            rawDigits.Contains($"55{phoneDigits}", StringComparison.Ordinal))
        {
            return true;
        }

        // Se o telefone tiver +55 mas o rawText nA£o, aceitar match sem o prefixo
        if (phoneDigits.StartsWith("55", StringComparison.Ordinal) && phoneDigits.Length is 12 or 13)
        {
            var withoutCountry = phoneDigits.Substring(2);
            if (rawDigits.Contains(withoutCountry, StringComparison.Ordinal))
            {
                return true;
            }
        }

        return false;
    }

    private static string DigitsOnly(string text)
    {
        return new string(text.Where(char.IsDigit).ToArray());
    }

    private static string? NormalizeBrazilPhoneForDomain(string? candidate)
    {
        if (string.IsNullOrWhiteSpace(candidate))
        {
            return null;
        }

        var digits = DigitsOnly(candidate);

        // Alguns OCRs colocam 55 (Brasil) no comeA§o; o domA-nio aceita apenas 10/11 dA-gitos (DDD + nA-mero).
        if (digits.StartsWith("55", StringComparison.Ordinal) && digits.Length is 12 or 13)
        {
            digits = digits.Substring(2);
        }

        // Aceita telefone fixo (10) ou celular (11) conforme PhoneNumber.Validate
        if (digits.Length is 10 or 11)
        {
            return digits;
        }

        return null;
    }

    private async Task<string?> TryExtractPhoneFromImageAsync(string imageBase64, CancellationToken cancellationToken)
    {
        try
        {
            var prompt = @"Extraia APENAS o nA-mero de telefone/celular do cartA£o de visita na imagem.

Retorne SOMENTE um JSON vA¡lido no formato:
{""phone"":""telefone""}

REGRAS:
- NUNCA invente. Se nA£o encontrar, use {""phone"":null}.
- Inclua apenas o nA-mero (com DDD). Se houver +55, pode incluir, mas nA£o inclua texto extra.";

            var response = await _ollamaClient.GenerateAsync(
                model: _model,
                prompt: prompt,
                imageBase64: imageBase64,
                temperature: 0.0,
                maxTokens: 150,
                cancellationToken: cancellationToken);

            var json = ExtractJsonFromResponse(response);
            if (string.IsNullOrWhiteSpace(json))
            {
                return null;
            }

            using var doc = JsonDocument.Parse(json);
            var root = doc.RootElement;
            var phone = ExtractStringProperty(root, "phone");
            return NormalizeBrazilPhoneForDomain(phone);
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "Second-pass phone OCR falhou (ignorando).");
            return null;
        }
    }

    private static string? GuessPersonNameFromText(string? rawText)
    {
        if (string.IsNullOrWhiteSpace(rawText))
        {
            return null;
        }

        var lines = rawText
            .Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Where(l => !string.IsNullOrWhiteSpace(l))
            .ToList();

        foreach (var line in lines)
        {
            if (line.Contains('@') ||
                line.Contains("www", StringComparison.OrdinalIgnoreCase) ||
                line.Contains(".com", StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            if (line.Any(char.IsDigit))
            {
                continue;
            }

            if (line.Contains("mobile", StringComparison.OrdinalIgnoreCase) ||
                line.Contains("cel", StringComparison.OrdinalIgnoreCase) ||
                line.Contains("tel", StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            var parts = line.Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
            if (parts.Length is < 2 or > 5)
            {
                continue;
            }

            if (line.Length <= 60 && parts.All(p => p.Length >= 2))
            {
                return line.Trim();
            }
        }

        return null;
    }

    private static string? GuessCompanyFromText(string? rawText)
    {
        if (string.IsNullOrWhiteSpace(rawText))
        {
            return null;
        }

        var lines = rawText
            .Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Where(l => !string.IsNullOrWhiteSpace(l))
            .ToList();

        foreach (var line in lines)
        {
            if (line.Contains('@') || line.Any(char.IsDigit))
            {
                continue;
            }

            if (line.Contains("www", StringComparison.OrdinalIgnoreCase) || line.Contains(".com", StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            if (line.Contains("av.", StringComparison.OrdinalIgnoreCase) ||
                line.Contains("rua", StringComparison.OrdinalIgnoreCase) ||
                line.Contains("andar", StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            if (line.Contains("mobile", StringComparison.OrdinalIgnoreCase) ||
                line.Contains("cel", StringComparison.OrdinalIgnoreCase) ||
                line.Contains("tel", StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            if (line.Length is >= 2 and <= 40)
            {
                return line.Trim();
            }
        }

        return null;
    }

    private static string? GuessJobTitleFromText(string? rawText, string? name, string? company)
    {
        if (string.IsNullOrWhiteSpace(rawText))
        {
            return null;
        }

        var lines = rawText
            .Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Where(l => !string.IsNullOrWhiteSpace(l))
            .ToList();

        foreach (var line in lines)
        {
            if (line.Contains('@') || line.Any(char.IsDigit))
            {
                continue;
            }

            if (!string.IsNullOrWhiteSpace(name) && line.Equals(name, StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            if (!string.IsNullOrWhiteSpace(company) && line.Equals(company, StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            if (line.Contains("www", StringComparison.OrdinalIgnoreCase) || line.Contains(".com", StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            if (line.Contains("av.", StringComparison.OrdinalIgnoreCase) ||
                line.Contains("rua", StringComparison.OrdinalIgnoreCase) ||
                line.Contains("andar", StringComparison.OrdinalIgnoreCase) ||
                line.Contains("brasil", StringComparison.OrdinalIgnoreCase) ||
                line.Contains("são paulo", StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            if (line.Contains("mobile", StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            if (line.Length is >= 3 and <= 60)
            {
                return line.Trim();
            }
        }

        return null;
    }

    private static bool ShouldRefineExtract(OcrExtract extract)
    {
        // Refinar se:
        // 1. RawText existe mas está vazio ou muito curto
        if (string.IsNullOrWhiteSpace(extract.RawText) || extract.RawText.Length < 10)
        {
            return false; // Não há texto suficiente para refinar
        }

        // 2. Menos de 3 campos foram preenchidos
        var filledFields = 0;
        if (!string.IsNullOrWhiteSpace(extract.Name)) filledFields++;
        if (!string.IsNullOrWhiteSpace(extract.Email)) filledFields++;
        if (!string.IsNullOrWhiteSpace(extract.Phone)) filledFields++;
        if (!string.IsNullOrWhiteSpace(extract.Company)) filledFields++;
        if (!string.IsNullOrWhiteSpace(extract.JobTitle)) filledFields++;

        if (filledFields < 3)
        {
            return true; // Poucos campos preenchidos, vale a pena refinar
        }

        // 3. Algum campo importante está faltando (email ou telefone)
        if (string.IsNullOrWhiteSpace(extract.Email) && string.IsNullOrWhiteSpace(extract.Phone))
        {
            return true; // Falta informação de contato essencial
        }

        // 4. Confiança média baixa (< 0.7)
        var avgConfidence = extract.ConfidenceScores.Values.DefaultIfEmpty(0m).Average();
        if (avgConfidence < 0.7m)
        {
            return true; // Confiança baixa, vale refinar
        }

        return false; // Extração parece boa, não precisa refinar
    }
}
