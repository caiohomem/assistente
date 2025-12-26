using AssistenteExecutivo.Application.Interfaces;
using AssistenteExecutivo.Domain.Interfaces;
using AssistenteExecutivo.Domain.ValueObjects;
using AssistenteExecutivo.Infrastructure.HttpClients;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace AssistenteExecutivo.Infrastructure.Services;

/// <summary>
/// Serviço de refinamento de campos OCR usando Qwen via Ollama
/// </summary>
public class QwenOcrRefinementService : IOcrFieldRefinementService
{
    private static readonly Regex EmailRegex = new(@"\b[A-Za-z0-9._%+-]+@[A-Za-z0-9.-]+\.[A-Z|a-z]{2,}\b", RegexOptions.IgnoreCase);
    private static readonly Regex PhoneRegex = new(@"(\+55\s?)?(\(?\d{2}\)?\s?)?(\d{4,5}[-.\s]?\d{4})");
    private static readonly Regex PhoneLikeChunkRegex = new(@"[\+0-9\(\)\-\.\s]{8,}");

    private readonly OllamaClient _ollamaClient;
    private readonly ILogger<QwenOcrRefinementService> _logger;
    private readonly IAgentConfigurationRepository _configurationRepository;
    private readonly string _model;
    private readonly double _temperature;
    private readonly int _maxTokens;

    public QwenOcrRefinementService(
        OllamaClient ollamaClient,
        IConfiguration configuration,
        ILogger<QwenOcrRefinementService> logger,
        IAgentConfigurationRepository configurationRepository)
    {
        _ollamaClient = ollamaClient;
        _logger = logger;
        _configurationRepository = configurationRepository;
        _model = configuration["Ollama:LLM:Model"] ?? "qwen2.5:7b";
        _temperature = double.Parse(configuration["Ollama:LLM:Temperature"] ?? "0.3");
        _maxTokens = int.Parse(configuration["Ollama:LLM:MaxTokens"] ?? "2000");
    }

    public async Task<OcrExtract> RefineFieldsAsync(
        string rawText,
        OcrExtract? initialExtract = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(rawText))
            {
                _logger.LogWarning("RawText vazio recebido para refinamento");
                return initialExtract ?? new OcrExtract();
            }

            _logger.LogInformation(
                "Refinando campos OCR usando modelo {Model}, RawText length={Length}",
                _model,
                rawText.Length);

            var prompt = await BuildPromptAsync(rawText, cancellationToken);
            var response = await _ollamaClient.ChatAsync(
                model: _model,
                message: prompt,
                temperature: _temperature,
                maxTokens: _maxTokens,
                cancellationToken: cancellationToken);

            _logger.LogDebug(
                "Resposta do Qwen (primeiros 500 chars): {Response}",
                response.Length > 500 ? response.Substring(0, 500) + "..." : response);

            var jsonText = ExtractJsonFromResponse(response);
            if (string.IsNullOrWhiteSpace(jsonText))
            {
                _logger.LogWarning("Não foi possível extrair JSON da resposta do Qwen, usando fallback");
                return FallbackToHeuristics(rawText, initialExtract, response);
            }

            var refinedExtract = ParseRefinedExtract(jsonText, rawText, response);
            
            // Merge com extração inicial: preservar campos já corretos se o LLM não melhorou
            var mergedExtract = MergeExtracts(initialExtract, refinedExtract, rawText, response);

            _logger.LogInformation(
                "Refinamento concluído: Name={Name}, Email={Email}, Phone={Phone}, Company={Company}, JobTitle={JobTitle}",
                mergedExtract.Name ?? "null",
                mergedExtract.Email ?? "null",
                mergedExtract.Phone ?? "null",
                mergedExtract.Company ?? "null",
                mergedExtract.JobTitle ?? "null");

            return mergedExtract;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao refinar campos OCR com Qwen, usando fallback");
            return FallbackToHeuristics(rawText, initialExtract, null);
        }
    }

    private async Task<string> BuildPromptAsync(string rawText, CancellationToken cancellationToken)
    {
        // Tentar obter o prompt de contexto da base de dados
        var configuration = await _configurationRepository.GetCurrentAsync(cancellationToken);
        
        string contextPrompt;
        if (configuration != null && !string.IsNullOrWhiteSpace(configuration.ContextPrompt))
        {
            contextPrompt = configuration.ContextPrompt;
            _logger.LogDebug("Usando prompt de contexto da base de dados");
        }
        else
        {
            // Fallback para o prompt padrão
            contextPrompt = GetDefaultContextPrompt();
            _logger.LogDebug("Usando prompt de contexto padrão (configuração não encontrada na base de dados)");
        }

        // Adicionar o rawText no final do prompt
        return $@"{contextPrompt}

TEXTO EXTRAÍDO:
{rawText}";
    }

    private static string GetDefaultContextPrompt()
    {
        return @"Analise o seguinte texto extraído de um cartão de visita e identifique os campos solicitados.

Extraia e retorne APENAS um JSON válido com esta estrutura:
{{
  ""name"": ""nome completo da pessoa ou null"",
  ""email"": ""email ou null"",
  ""phone"": ""telefone (apenas dígitos com DDD, sem +55) ou null"",
  ""company"": ""nome da empresa ou null"",
  ""jobTitle"": ""cargo/função ou null""
}}

REGRAS:
- NUNCA invente informações. Se não encontrar, use null.
- CORREÇÃO ORTOGRÁFICA: O texto pode conter erros de OCR (letras mal reconhecidas). Faça correção ortográfica MÍNIMA apenas para corrigir erros óbvios de reconhecimento de caracteres, mantendo o máximo possível do texto original.
  Exemplos de correções: ""Secretria"" -> ""Secretaria"", ""Teanologia"" -> ""Tecnologia"", ""Inovacäo"" -> ""Inovação"", ""Cisnoia"" -> ""Ciência"", ""Transformagäo"" -> ""Transformação"".
  NÃO altere nomes próprios, URLs, emails ou números de telefone (exceto para normalizar formato).
- Os valores devem corresponder ao texto original (após correção ortográfica mínima).
- Telefone: apenas dígitos (DDD + número, 10 ou 11 dígitos). Se houver +55, remova. Normalize removendo espaços, pontos e hífens, mas mantenha apenas os dígitos.
- Email: formato válido de email. Mantenha exatamente como aparece, apenas corrija erros óbvios de OCR se necessário.
- Nome: geralmente aparece no topo do cartão, pode ter 2-5 palavras. Corrija apenas erros óbvios de OCR.
- Empresa: geralmente aparece abaixo do nome ou em linha separada. Corrija apenas erros óbvios de OCR.
  **VALIDAÇÃO CRÍTICA DO NOME DA EMPRESA**: Se houver um email no texto, SEMPRE extraia o domínio do email (parte após o @, antes do .com/.com.br/etc) e use-o como referência para validar e corrigir o nome da empresa. Quase sempre o nome da empresa aparece no domínio do email. 
  Exemplo: se o email é ""joao@spacemoon.com.br"" e o texto OCR mostra ""SPACEMOn"", corrija para ""SpaceMoon"" (ou a grafia correta baseada no domínio ""spacemoon""). 
  Se o nome da empresa no texto não corresponder ao domínio do email, PREFIRA usar a grafia do domínio do email como fonte confiável, ajustando apenas capitalização se necessário.
- Cargo: geralmente aparece entre nome e empresa, ou abaixo do nome. Corrija apenas erros óbvios de OCR.
- Retorne SOMENTE o JSON, sem markdown, sem explicações, sem texto adicional.";
    }

    private string ExtractJsonFromResponse(string response)
    {
        var cleaned = response.Trim();

        // Tentar encontrar JSON em markdown code block
        var markdownMatch = Regex.Match(
            cleaned,
            @"```(?:json)?\s*(\{.*?\})\s*```",
            RegexOptions.Singleline | RegexOptions.IgnoreCase);

        if (markdownMatch.Success)
        {
            cleaned = markdownMatch.Groups[1].Value.Trim();
        }

        // Remover prefixos comuns
        cleaned = Regex.Replace(
            cleaned,
            @"^(?:Aqui está|Here is|JSON:|Resposta:|Resultado:)\s*",
            "",
            RegexOptions.IgnoreCase | RegexOptions.Multiline);

        // Encontrar primeiro { e último }
        var jsonStart = cleaned.IndexOf('{');
        var jsonEnd = cleaned.LastIndexOf('}');

        if (jsonStart >= 0 && jsonEnd > jsonStart)
        {
            cleaned = cleaned.Substring(jsonStart, jsonEnd - jsonStart + 1);
        }

        // Validar JSON
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
                return string.Empty;
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

    private OcrExtract ParseRefinedExtract(string jsonText, string rawText, string? aiRawResponse)
    {
        try
        {
            using var doc = JsonDocument.Parse(jsonText);
            var root = doc.RootElement;

            var name = ExtractStringProperty(root, "name");
            var email = ExtractStringProperty(root, "email");
            var phone = ExtractStringProperty(root, "phone");
            var company = ExtractStringProperty(root, "company");
            var jobTitle = ExtractStringProperty(root, "jobTitle");

            // Validar que os valores aparecem no texto original
            if (!string.IsNullOrWhiteSpace(rawText))
            {
                name = ValidateAndCleanField(name, rawText, isName: true);
                email = ValidateAndCleanEmail(email, rawText);
                phone = ValidateAndCleanPhone(phone, rawText);
                company = ValidateAndCleanField(company, rawText, isName: false);
                jobTitle = ValidateAndCleanField(jobTitle, rawText, isName: false);
            }

            // Se validação falhou, tentar extrair via regex como fallback
            email ??= ExtractEmailFromText(rawText);
            phone ??= ExtractPhoneFromText(rawText);

            // Validar e corrigir nome da empresa usando o domínio do email
            if (!string.IsNullOrWhiteSpace(email) && !string.IsNullOrWhiteSpace(company))
            {
                company = ValidateCompanyAgainstEmailDomain(company, email, rawText);
            }
            else if (!string.IsNullOrWhiteSpace(email) && string.IsNullOrWhiteSpace(company))
            {
                // Tentar extrair nome da empresa do domínio do email
                company = ExtractCompanyNameFromEmailDomain(email, rawText);
            }

            var confidenceScores = new Dictionary<string, decimal>
            {
                { "name", name != null ? 0.9m : 0m },
                { "email", email != null ? 0.95m : 0m },
                { "phone", phone != null ? 0.9m : 0m },
                { "company", company != null ? 0.85m : 0m },
                { "jobTitle", jobTitle != null ? 0.8m : 0m }
            };

            return new OcrExtract(
                rawText: rawText,
                name: name,
                email: email,
                phone: phone,
                company: company,
                jobTitle: jobTitle,
                confidenceScores: confidenceScores,
                aiRawResponse: aiRawResponse);
        }
        catch (JsonException ex)
        {
            _logger.LogWarning(ex, "Erro ao fazer parse do JSON refinado: {JsonText}", jsonText);
            return FallbackToHeuristics(rawText, null, aiRawResponse);
        }
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

    private static string? ValidateAndCleanField(string? value, string rawText, bool isName)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        // Verificar se aparece no texto original (com tolerância para variações)
        if (!AppearsInRawText(value, rawText))
        {
            return null;
        }

        // Limpar caracteres inválidos
        value = value.Trim();
        
        // Para nomes, remover caracteres muito estranhos
        if (isName && value.Any(c => char.IsDigit(c) && !char.IsLetter(c)))
        {
            // Se tem muitos dígitos, provavelmente não é um nome
            if (value.Count(char.IsDigit) > value.Length / 3)
            {
                return null;
            }
        }

        return value;
    }

    private static string? ValidateAndCleanEmail(string? email, string rawText)
    {
        if (string.IsNullOrWhiteSpace(email))
        {
            return null;
        }

        if (!IsValidEmail(email))
        {
            return null;
        }

        if (!AppearsInRawText(email, rawText))
        {
            return null;
        }

        return email.Trim();
    }

    private static string? ValidateAndCleanPhone(string? phone, string rawText)
    {
        if (string.IsNullOrWhiteSpace(phone))
        {
            return null;
        }

        // Normalizar telefone: remover caracteres não numéricos, exceto se for apenas dígitos
        var digits = DigitsOnly(phone);
        
        // Remover código do país se presente
        if (digits.StartsWith("55", StringComparison.Ordinal) && digits.Length is 12 or 13)
        {
            digits = digits.Substring(2);
        }

        // Validar formato (10 ou 11 dígitos)
        if (digits.Length is not (10 or 11))
        {
            return null;
        }

        // Verificar se aparece no texto original
        if (!PhoneAppearsInRawText(digits, rawText))
        {
            return null;
        }

        return digits;
    }

    private static bool AppearsInRawText(string? value, string rawText)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return false;
        }

        // Busca exata (case-insensitive)
        if (rawText.IndexOf(value, StringComparison.OrdinalIgnoreCase) >= 0)
        {
            return true;
        }

        // Busca por palavras (para nomes compostos)
        var valueWords = value.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        if (valueWords.Length > 1)
        {
            var allWordsFound = valueWords.All(word => 
                rawText.IndexOf(word, StringComparison.OrdinalIgnoreCase) >= 0);
            if (allWordsFound)
            {
                return true;
            }
        }

        return false;
    }

    private static bool PhoneAppearsInRawText(string phoneDigits, string rawText)
    {
        if (phoneDigits.Length < 8)
        {
            return false;
        }

        var rawDigits = DigitsOnly(rawText);
        if (rawDigits.Contains(phoneDigits, StringComparison.Ordinal))
        {
            return true;
        }

        // Se o rawText tiver código do país +55, aceitar match com prefixo
        if (!phoneDigits.StartsWith("55", StringComparison.Ordinal) &&
            rawDigits.Contains($"55{phoneDigits}", StringComparison.Ordinal))
        {
            return true;
        }

        // Se o telefone tiver +55 mas o rawText não, aceitar match sem o prefixo
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

    private static string? ValidateCompanyAgainstEmailDomain(string? company, string email, string rawText)
    {
        if (string.IsNullOrWhiteSpace(company) || string.IsNullOrWhiteSpace(email))
        {
            return company;
        }

        // Extrair domínio do email (parte após @, antes do primeiro ponto do TLD)
        var atIndex = email.IndexOf('@');
        if (atIndex < 0 || atIndex >= email.Length - 1)
        {
            return company;
        }

        var domainPart = email.Substring(atIndex + 1);
        var firstDotIndex = domainPart.IndexOf('.');
        var domainName = firstDotIndex > 0 
            ? domainPart.Substring(0, firstDotIndex) 
            : domainPart;

        if (string.IsNullOrWhiteSpace(domainName))
        {
            return company;
        }

        // Normalizar para comparação (remover espaços, converter para minúsculas)
        var normalizedDomain = domainName.ToLowerInvariant().Trim();
        var normalizedCompany = company.ToLowerInvariant().Trim();

        // Verificar se o domínio contém o nome da empresa ou vice-versa
        // Exemplo: "spacemoon" no domínio vs "SPACEMOn" na empresa
        if (normalizedDomain.Contains(normalizedCompany) || normalizedCompany.Contains(normalizedDomain))
        {
            // Se são similares, usar o domínio como referência mas manter capitalização do texto original se possível
            // Tentar encontrar no texto original uma versão que corresponda melhor ao domínio
            var correctedCompany = FindBestCompanyMatchInText(domainName, rawText);
            if (!string.IsNullOrWhiteSpace(correctedCompany))
            {
                return correctedCompany;
            }

            // Se não encontrar no texto, usar o domínio com capitalização apropriada
            return CapitalizeCompanyName(domainName);
        }

        // Se não corresponder, mas o domínio parece ser um nome de empresa válido, usar o domínio
        // (pode ser que o OCR tenha errado completamente o nome da empresa)
        if (domainName.Length >= 3 && domainName.All(c => char.IsLetter(c) || char.IsDigit(c)))
        {
            var correctedCompany = FindBestCompanyMatchInText(domainName, rawText);
            if (!string.IsNullOrWhiteSpace(correctedCompany))
            {
                return correctedCompany;
            }
            return CapitalizeCompanyName(domainName);
        }

        return company;
    }

    private static string? ExtractCompanyNameFromEmailDomain(string email, string rawText)
    {
        if (string.IsNullOrWhiteSpace(email))
        {
            return null;
        }

        var atIndex = email.IndexOf('@');
        if (atIndex < 0 || atIndex >= email.Length - 1)
        {
            return null;
        }

        var domainPart = email.Substring(atIndex + 1);
        var firstDotIndex = domainPart.IndexOf('.');
        var domainName = firstDotIndex > 0 
            ? domainPart.Substring(0, firstDotIndex) 
            : domainPart;

        if (string.IsNullOrWhiteSpace(domainName) || domainName.Length < 3)
        {
            return null;
        }

        // Tentar encontrar no texto original uma versão que corresponda ao domínio
        var companyFromText = FindBestCompanyMatchInText(domainName, rawText);
        if (!string.IsNullOrWhiteSpace(companyFromText))
        {
            return companyFromText;
        }

        // Se não encontrar, usar o domínio com capitalização apropriada
        return CapitalizeCompanyName(domainName);
    }

    private static string? FindBestCompanyMatchInText(string domainName, string rawText)
    {
        if (string.IsNullOrWhiteSpace(domainName) || string.IsNullOrWhiteSpace(rawText))
        {
            return null;
        }

        var normalizedDomain = domainName.ToLowerInvariant();
        var lines = rawText
            .Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Where(l => !string.IsNullOrWhiteSpace(l))
            .ToList();

        foreach (var line in lines)
        {
            // Ignorar linhas que são claramente emails, telefones, etc
            if (line.Contains('@') || 
                line.Any(char.IsDigit) && DigitsOnly(line).Length >= 8 ||
                line.Contains("www", StringComparison.OrdinalIgnoreCase) ||
                line.Contains(".com", StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            var normalizedLine = line.ToLowerInvariant();
            
            // Verificar se a linha contém o domínio (com tolerância para erros de OCR)
            if (normalizedLine.Contains(normalizedDomain) || 
                StringsAreSimilar(normalizedLine, normalizedDomain))
            {
                // Retornar a linha original (com capitalização preservada)
                return line.Trim();
            }
        }

        return null;
    }

    private static bool StringsAreSimilar(string s1, string s2)
    {
        if (string.IsNullOrWhiteSpace(s1) || string.IsNullOrWhiteSpace(s2))
        {
            return false;
        }

        // Verificar se uma string contém a maior parte da outra
        var minLength = Math.Min(s1.Length, s2.Length);
        var maxLength = Math.Max(s1.Length, s2.Length);
        
        if (minLength < 3)
        {
            return false;
        }

        // Se o comprimento é muito diferente, não são similares
        if (maxLength > minLength * 1.5)
        {
            return false;
        }

        // Contar caracteres comuns
        var commonChars = 0;
        foreach (var c in s1)
        {
            if (s2.Contains(c))
            {
                commonChars++;
            }
        }

        // Se pelo menos 70% dos caracteres são comuns, são similares
        return commonChars >= minLength * 0.7;
    }

    private static string CapitalizeCompanyName(string domainName)
    {
        if (string.IsNullOrWhiteSpace(domainName))
        {
            return domainName;
        }

        // Capitalizar primeira letra de cada palavra (assumindo camelCase ou palavras separadas)
        // Exemplo: "spacemoon" -> "SpaceMoon", "acme-corp" -> "Acme-Corp"
        var parts = domainName.Split(new[] { '-', '_', '.' }, StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length > 1)
        {
            return string.Join("-", parts.Select(p => 
                p.Length > 0 ? char.ToUpperInvariant(p[0]) + p.Substring(1).ToLowerInvariant() : p));
        }

        // Se for uma palavra só, tentar detectar camelCase ou capitalizar primeira letra
        if (domainName.Any(char.IsUpper))
        {
            // Já tem capitalização, manter
            return domainName;
        }

        // Capitalizar primeira letra
        return domainName.Length > 0 
            ? char.ToUpperInvariant(domainName[0]) + domainName.Substring(1).ToLowerInvariant() 
            : domainName;
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

        // Tentar regex direto
        var match = PhoneRegex.Match(text);
        if (match.Success)
        {
            var normalized = NormalizeBrazilPhoneForDomain(match.Value);
            if (normalized != null)
            {
                return normalized;
            }
        }

        // Fallback: procurar "chunks" com cara de telefone
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

    private static string? NormalizeBrazilPhoneForDomain(string? candidate)
    {
        if (string.IsNullOrWhiteSpace(candidate))
        {
            return null;
        }

        var digits = DigitsOnly(candidate);

        // Remover código do país se presente
        if (digits.StartsWith("55", StringComparison.Ordinal) && digits.Length is 12 or 13)
        {
            digits = digits.Substring(2);
        }

        // Aceita telefone fixo (10) ou celular (11)
        if (digits.Length is 10 or 11)
        {
            return digits;
        }

        return null;
    }

    private OcrExtract FallbackToHeuristics(string rawText, OcrExtract? initialExtract, string? aiRawResponse)
    {
        _logger.LogInformation("Usando heurísticas como fallback para refinamento");

        var email = ExtractEmailFromText(rawText);
        var phone = ExtractPhoneFromText(rawText);
        var name = GuessPersonNameFromText(rawText);
        var company = GuessCompanyFromText(rawText);
        var jobTitle = GuessJobTitleFromText(rawText, name, company);

        var confidenceScores = new Dictionary<string, decimal>
        {
            { "name", name != null ? 0.6m : 0m },
            { "email", email != null ? 0.9m : 0m },
            { "phone", phone != null ? 0.8m : 0m },
            { "company", company != null ? 0.7m : 0m },
            { "jobTitle", jobTitle != null ? 0.6m : 0m }
        };

        var fallbackExtract = new OcrExtract(
            rawText: rawText,
            name: name,
            email: email,
            phone: phone,
            company: company,
            jobTitle: jobTitle,
            confidenceScores: confidenceScores,
            aiRawResponse: aiRawResponse);

        // Merge com extração inicial se disponível
        return MergeExtracts(initialExtract, fallbackExtract, rawText, aiRawResponse);
    }

    private static OcrExtract MergeExtracts(OcrExtract? initial, OcrExtract refined, string rawText, string? aiRawResponse)
    {
        if (initial == null)
        {
            return refined;
        }

        // Estratégia: usar o melhor valor entre inicial e refinado
        // Preferir refinado se tiver maior confiança, senão manter inicial se já estava correto

        var name = SelectBestValue(initial.Name, refined.Name, initial.ConfidenceScores.GetValueOrDefault("name"), refined.ConfidenceScores.GetValueOrDefault("name"), rawText);
        var email = SelectBestValue(initial.Email, refined.Email, initial.ConfidenceScores.GetValueOrDefault("email"), refined.ConfidenceScores.GetValueOrDefault("email"), rawText);
        var phone = SelectBestValue(initial.Phone, refined.Phone, initial.ConfidenceScores.GetValueOrDefault("phone"), refined.ConfidenceScores.GetValueOrDefault("phone"), rawText);
        var company = SelectBestValue(initial.Company, refined.Company, initial.ConfidenceScores.GetValueOrDefault("company"), refined.ConfidenceScores.GetValueOrDefault("company"), rawText);
        var jobTitle = SelectBestValue(initial.JobTitle, refined.JobTitle, initial.ConfidenceScores.GetValueOrDefault("jobTitle"), refined.ConfidenceScores.GetValueOrDefault("jobTitle"), rawText);

        var mergedScores = new Dictionary<string, decimal>
        {
            { "name", name != null ? Math.Max(initial.ConfidenceScores.GetValueOrDefault("name"), refined.ConfidenceScores.GetValueOrDefault("name")) : 0m },
            { "email", email != null ? Math.Max(initial.ConfidenceScores.GetValueOrDefault("email"), refined.ConfidenceScores.GetValueOrDefault("email")) : 0m },
            { "phone", phone != null ? Math.Max(initial.ConfidenceScores.GetValueOrDefault("phone"), refined.ConfidenceScores.GetValueOrDefault("phone")) : 0m },
            { "company", company != null ? Math.Max(initial.ConfidenceScores.GetValueOrDefault("company"), refined.ConfidenceScores.GetValueOrDefault("company")) : 0m },
            { "jobTitle", jobTitle != null ? Math.Max(initial.ConfidenceScores.GetValueOrDefault("jobTitle"), refined.ConfidenceScores.GetValueOrDefault("jobTitle")) : 0m }
        };

        // Preservar aiRawResponse do refined se disponível, senão usar o passado como parâmetro
        var finalAiRawResponse = refined.AiRawResponse ?? aiRawResponse ?? initial.AiRawResponse;

        return new OcrExtract(
            rawText: rawText,
            name: name,
            email: email,
            phone: phone,
            company: company,
            jobTitle: jobTitle,
            confidenceScores: mergedScores,
            aiRawResponse: finalAiRawResponse);
    }

    private static string? SelectBestValue(string? initial, string? refined, decimal initialConfidence, decimal refinedConfidence, string rawText)
    {
        // Se refinado tem maior confiança e não é nulo, usar refinado
        if (!string.IsNullOrWhiteSpace(refined) && refinedConfidence > initialConfidence)
        {
            return refined;
        }

        // Se inicial existe e refinado não, manter inicial
        if (!string.IsNullOrWhiteSpace(initial) && string.IsNullOrWhiteSpace(refined))
        {
            return initial;
        }

        // Se ambos existem, preferir o que tem maior confiança
        if (!string.IsNullOrWhiteSpace(initial) && !string.IsNullOrWhiteSpace(refined))
        {
            return refinedConfidence >= initialConfidence ? refined : initial;
        }

        // Se apenas refinado existe, usar refinado
        return refined;
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
}

