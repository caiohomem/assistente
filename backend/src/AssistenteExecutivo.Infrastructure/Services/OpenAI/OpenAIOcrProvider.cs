using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using AssistenteExecutivo.Application.Interfaces;
using AssistenteExecutivo.Domain.Interfaces;
using AssistenteExecutivo.Domain.ValueObjects;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace AssistenteExecutivo.Infrastructure.Services.OpenAI;

public sealed class OpenAIOcrProvider : IOcrProvider
{
    private static readonly Regex EmailRegex = new(@"\b[A-Za-z0-9._%+-]+@[A-Za-z0-9.-]+\.[A-Z|a-z]{2,}\b", RegexOptions.IgnoreCase);
    private static readonly Regex PhoneRegex = new(@"(\+55\s?)?(\(?\d{2}\)?\s?)?(\d{4,5}[-.\s]?\d{4})");

    private readonly HttpClient _httpClient;
    private readonly ILogger<OpenAIOcrProvider> _logger;
    private readonly IAgentConfigurationRepository _configurationRepository;
    private readonly string _model;
    private readonly double _temperature;
    private readonly int _maxTokens;
    private readonly string _apiKey;

    public OpenAIOcrProvider(
        IConfiguration configuration,
        IHttpClientFactory httpClientFactory,
        ILogger<OpenAIOcrProvider> logger,
        IAgentConfigurationRepository configurationRepository)
    {
        _logger = logger;
        _configurationRepository = configurationRepository;
        _httpClient = httpClientFactory.CreateClient();
        
        var baseUrl = configuration["OpenAI:BaseUrl"] ?? "https://api.openai.com/v1/";
        _httpClient.BaseAddress = new Uri(baseUrl);
        _httpClient.Timeout = TimeSpan.FromMinutes(10);
        
        _apiKey = configuration["OpenAI:ApiKey"] 
            ?? throw new InvalidOperationException("OpenAI:ApiKey não configurado");
        
        _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _apiKey);
        
        var organizationId = configuration["OpenAI:OrganizationId"];
        if (!string.IsNullOrWhiteSpace(organizationId))
        {
            _httpClient.DefaultRequestHeaders.Add("OpenAI-Organization", organizationId);
        }
        
        _model = configuration["OpenAI:Ocr:Model"] ?? "gpt-4o-mini";
        var temperatureValue = double.Parse(configuration["OpenAI:Ocr:Temperature"] ?? "0.0");
        // OpenAI API requires temperature to be between 0 and 2
        if (temperatureValue < 0)
        {
            _logger.LogWarning("Temperature value {Temperature} is below minimum (0), clamping to 0", temperatureValue);
            _temperature = 0;
        }
        else if (temperatureValue > 2)
        {
            _logger.LogWarning("Temperature value {Temperature} exceeds maximum (2), clamping to 2", temperatureValue);
            _temperature = 2;
        }
        else
        {
            _temperature = temperatureValue;
        }
        _maxTokens = int.Parse(configuration["OpenAI:Ocr:MaxTokens"] ?? "500");
    }

    public async Task<OcrExtract> ExtractFieldsAsync(
        byte[] imageBytes,
        string mimeType,
        CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation(
                "Processando imagem OCR com OpenAI. Tamanho: {Size} bytes, MimeType: {MimeType}, Model: {Model}",
                imageBytes.Length, mimeType, _model);

            var prompt = await BuildPromptAsync(cancellationToken);
            var base64Image = Convert.ToBase64String(imageBytes);
            
            var requestBody = new
            {
                model = _model,
                messages = new[]
                {
                    new
                    {
                        role = "user",
                        content = new object[]
                        {
                            new { type = "text", text = prompt },
                            new
                            {
                                type = "image_url",
                                image_url = new { url = $"data:{mimeType};base64,{base64Image}" }
                            }
                        }
                    }
                },
                max_tokens = _maxTokens,
                temperature = _temperature,
                response_format = new { type = "json_object" }
            };

            var json = JsonSerializer.Serialize(requestBody);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            var response = await _httpClient.PostAsync("chat/completions", content, cancellationToken);
            response.EnsureSuccessStatusCode();

            var responseJson = await response.Content.ReadAsStringAsync(cancellationToken);
            var jsonDoc = JsonDocument.Parse(responseJson);
            
            var responseText = jsonDoc.RootElement
                .GetProperty("choices")[0]
                .GetProperty("message")
                .GetProperty("content")
                .GetString() ?? string.Empty;
            
            _logger.LogDebug("Resposta do OpenAI OCR: {Response}", responseText);

            var extract = ParseResponse(responseText, imageBytes.Length);
            
            _logger.LogInformation(
                "OCR concluído: Name={Name}, Email={Email}, Phone={Phone}, Company={Company}, JobTitle={JobTitle}",
                extract.Name ?? "null",
                extract.Email ?? "null",
                extract.Phone ?? "null",
                extract.Company ?? "null",
                extract.JobTitle ?? "null");

            return extract;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao processar OCR com OpenAI");
            throw;
        }
    }

    private async Task<string> BuildPromptAsync(CancellationToken cancellationToken)
    {
        // Tentar obter o prompt de OCR da base de dados
        var configuration = await _configurationRepository.GetCurrentAsync(cancellationToken);
        
        string ocrPrompt;
        if (configuration != null && !string.IsNullOrWhiteSpace(configuration.OcrPrompt))
        {
            ocrPrompt = configuration.OcrPrompt;
            _logger.LogDebug("Usando prompt de OCR da base de dados");
        }
        else
        {
            // Fallback para o prompt padrão
            ocrPrompt = GetDefaultOcrPrompt();
            _logger.LogDebug("Usando prompt de OCR padrão (configuração não encontrada na base de dados)");
        }

        return ocrPrompt;
    }

    private static string GetDefaultOcrPrompt()
    {
        return @"Analise esta imagem de um cartão de visita e extraia as seguintes informações em formato JSON válido:
{
  ""name"": ""nome completo da pessoa"",
  ""email"": ""endereço de email"",
  ""phone"": ""telefone (formato brasileiro, apenas números)"",
  ""company"": ""nome da empresa"",
  ""jobTitle"": ""cargo/função"",
  ""rawText"": ""todo o texto visível no cartão""
}

Extraia apenas informações que estejam claramente visíveis na imagem. Se algum campo não estiver presente, use null. 
Para o telefone, normalize para formato brasileiro (apenas números, sem espaços ou caracteres especiais).
Retorne APENAS o JSON, sem markdown, sem explicações adicionais.";
    }

    private OcrExtract ParseResponse(string responseText, int imageSize)
    {
        try
        {
            // Limpar resposta (remover markdown code blocks se houver)
            var cleanedText = responseText.Trim();
            if (cleanedText.StartsWith("```json", StringComparison.OrdinalIgnoreCase))
            {
                cleanedText = cleanedText.Substring(7);
            }
            if (cleanedText.StartsWith("```", StringComparison.OrdinalIgnoreCase))
            {
                cleanedText = cleanedText.Substring(3);
            }
            if (cleanedText.EndsWith("```", StringComparison.OrdinalIgnoreCase))
            {
                cleanedText = cleanedText.Substring(0, cleanedText.Length - 3);
            }
            cleanedText = cleanedText.Trim();

            using var doc = JsonDocument.Parse(cleanedText);
            var root = doc.RootElement;

            var rawText = root.TryGetProperty("rawText", out var rawTextProp)
                ? rawTextProp.GetString()?.Trim()
                : null;

            var name = root.TryGetProperty("name", out var nameProp)
                ? nameProp.GetString()?.Trim()
                : null;

            var email = root.TryGetProperty("email", out var emailProp)
                ? emailProp.GetString()?.Trim()
                : null;

            // Se email não veio do JSON, tentar extrair do rawText
            if (string.IsNullOrWhiteSpace(email) && !string.IsNullOrWhiteSpace(rawText))
            {
                var emailMatch = EmailRegex.Match(rawText);
                email = emailMatch.Success ? emailMatch.Value.Trim() : null;
            }

            var phone = root.TryGetProperty("phone", out var phoneProp)
                ? phoneProp.GetString()?.Trim()
                : null;

            // Normalizar telefone (remover caracteres não numéricos, exceto +)
            if (!string.IsNullOrWhiteSpace(phone))
            {
                phone = NormalizeBrazilPhone(phone);
            }
            else if (!string.IsNullOrWhiteSpace(rawText))
            {
                // Tentar extrair telefone do rawText
                var phoneMatch = PhoneRegex.Match(rawText);
                if (phoneMatch.Success)
                {
                    phone = NormalizeBrazilPhone(phoneMatch.Value);
                }
            }

            var company = root.TryGetProperty("company", out var companyProp)
                ? companyProp.GetString()?.Trim()
                : null;

            var jobTitle = root.TryGetProperty("jobTitle", out var jobTitleProp)
                ? jobTitleProp.GetString()?.Trim()
                : null;

            var confidenceScores = new Dictionary<string, decimal>
            {
                { "name", name != null ? 0.9m : 0m },
                { "email", email != null ? 0.95m : 0m },
                { "phone", phone != null ? 0.95m : 0m },
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
                aiRawResponse: responseText);
        }
        catch (JsonException ex)
        {
            _logger.LogError(ex, "Erro ao fazer parse do JSON da resposta OpenAI. Resposta: {Response}", responseText);
            throw new InvalidOperationException($"Resposta inválida da OpenAI OCR: {ex.Message}", ex);
        }
    }

    private static string? NormalizeBrazilPhone(string phone)
    {
        if (string.IsNullOrWhiteSpace(phone))
        {
            return null;
        }

        // Remover todos os caracteres não numéricos, exceto +
        var digits = new string(phone.Where(c => char.IsDigit(c) || c == '+').ToArray());

        // Remover código do país se presente (55)
        if (digits.StartsWith("+55", StringComparison.Ordinal) && digits.Length is 13 or 14)
        {
            digits = digits.Substring(3);
        }
        else if (digits.StartsWith("55", StringComparison.Ordinal) && digits.Length is 12 or 13)
        {
            digits = digits.Substring(2);
        }

        // Validar tamanho (10 ou 11 dígitos para telefone brasileiro)
        return digits.Length is 10 or 11 ? digits : null;
    }
}
