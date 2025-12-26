using System.Text.RegularExpressions;
using AssistenteExecutivo.Domain.Interfaces;
using AssistenteExecutivo.Domain.ValueObjects;
using AssistenteExecutivo.Infrastructure.HttpClients;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace AssistenteExecutivo.Infrastructure.Services;

public sealed class PaddleOcrProvider : IOcrProvider
{
    private static readonly Regex EmailRegex = new(@"\b[A-Za-z0-9._%+-]+@[A-Za-z0-9.-]+\.[A-Z|a-z]{2,}\b", RegexOptions.IgnoreCase);
    private static readonly Regex PhoneChunkRegex = new(@"[\+0-9\(\)\-\.\s]{8,}");

    private readonly PaddleOcrApiClient _client;
    private readonly ILogger<PaddleOcrProvider> _logger;
    private readonly IOcrFieldRefinementService? _refinementService;
    private readonly string _lang;

    public PaddleOcrProvider(
        PaddleOcrApiClient client,
        IConfiguration configuration,
        ILogger<PaddleOcrProvider> logger,
        IOcrFieldRefinementService? refinementService = null)
    {
        _client = client;
        _logger = logger;
        _refinementService = refinementService;
        _lang = configuration["Ocr:PaddleOcr:Lang"] ?? "pt";
    }

    public async Task<OcrExtract> ExtractFieldsAsync(
        byte[] imageBytes,
        string mimeType,
        CancellationToken cancellationToken = default)
    {
        var fileName = mimeType switch
        {
            "image/jpeg" or "image/jpg" => "upload.jpg",
            "image/png" => "upload.png",
            "image/webp" => "upload.webp",
            _ => "upload.bin"
        };

        var response = await _client.OcrAsync(
            imageBytes: imageBytes,
            fileName: fileName,
            mimeType: mimeType,
            lang: _lang,
            cancellationToken: cancellationToken);

        var rawText = response.RawText?.Trim() ?? string.Empty;
        _logger.LogInformation(
            "PaddleOCR rawText (primeiros 500 chars): {RawText}",
            rawText.Length > 500 ? rawText.Substring(0, 500) + "..." : rawText);

        var email = ExtractEmail(rawText);
        var phone = ExtractBrazilPhone(rawText);

        var lines = rawText
            .Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .ToList();

        var name = GuessName(lines);
        var company = GuessCompany(lines, name);
        var jobTitle = GuessJobTitle(lines, name, company);

        var confidenceScores = new Dictionary<string, decimal>
        {
            { "name", name != null ? 0.85m : 0m },
            { "email", email != null ? 0.95m : 0m },
            { "phone", phone != null ? 0.95m : 0m },
            { "company", company != null ? 0.8m : 0m },
            { "jobTitle", jobTitle != null ? 0.75m : 0m }
        };

        var extract = new OcrExtract(
            rawText: rawText,
            name: name,
            email: email,
            phone: phone,
            company: company,
            jobTitle: jobTitle,
            confidenceScores: confidenceScores);

        // Refinamento com Qwen: usar se campos estiverem incompletos ou com baixa confiança
        //if (_refinementService != null && ShouldRefineExtract(extract))
        //{
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
        //}

        return extract;
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

    private static string? ExtractEmail(string rawText)
    {
        if (string.IsNullOrWhiteSpace(rawText))
        {
            return null;
        }

        // Common OCR mistake: "Sales@garter.com" -> "Sales@garter.com" (typo), still valid
        var match = EmailRegex.Match(rawText);
        return match.Success ? match.Value.Trim() : null;
    }

    private static string? ExtractBrazilPhone(string rawText)
    {
        if (string.IsNullOrWhiteSpace(rawText))
        {
            return null;
        }

        string? best = null;

        foreach (Match chunk in PhoneChunkRegex.Matches(rawText))
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

        var digits = new string(candidate.Where(char.IsDigit).ToArray());

        // Remove country code if present; domain accepts only DDD+number (10/11 digits)
        if (digits.StartsWith("55", StringComparison.Ordinal) && digits.Length is 12 or 13)
        {
            digits = digits.Substring(2);
        }

        return digits.Length is 10 or 11 ? digits : null;
    }

    private static string? GuessName(IReadOnlyList<string> lines)
    {
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

            var parts = line.Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
            if (parts.Length is < 2 or > 5)
            {
                continue;
            }

            if (line.Length <= 60)
            {
                return line.Trim();
            }
        }

        return null;
    }

    private static string? GuessCompany(IReadOnlyList<string> lines, string? name)
    {
        foreach (var line in lines)
        {
            if (!string.IsNullOrWhiteSpace(name) && line.Equals(name, StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            if (line.Contains('@') || line.Any(char.IsDigit))
            {
                continue;
            }

            if (line.Contains("www", StringComparison.OrdinalIgnoreCase) || line.Contains(".com", StringComparison.OrdinalIgnoreCase))
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

    private static string? GuessJobTitle(IReadOnlyList<string> lines, string? name, string? company)
    {
        foreach (var line in lines)
        {
            if (!string.IsNullOrWhiteSpace(name) && line.Equals(name, StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            if (!string.IsNullOrWhiteSpace(company) && line.Equals(company, StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            if (line.Contains('@') || line.Any(char.IsDigit))
            {
                continue;
            }

            if (line.Contains("www", StringComparison.OrdinalIgnoreCase) || line.Contains(".com", StringComparison.OrdinalIgnoreCase))
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
