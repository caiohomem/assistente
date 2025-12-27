using System.Linq;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using AssistenteExecutivo.Domain.Interfaces;
using AssistenteExecutivo.Domain.ValueObjects;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace AssistenteExecutivo.Infrastructure.Services.OpenAI;

public sealed class OpenAILLMProvider : ILLMProvider
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<OpenAILLMProvider> _logger;
    private readonly string _model;
    private readonly double _temperature;
    private readonly int _maxTokens;
    private readonly string _apiKey;

    public OpenAILLMProvider(
        IConfiguration configuration,
        IHttpClientFactory httpClientFactory,
        ILogger<OpenAILLMProvider> logger)
    {
        _logger = logger;
        _httpClient = httpClientFactory.CreateClient();
        
        var baseUrl = configuration["OpenAI:BaseUrl"] ?? "https://api.openai.com/v1/";
        _httpClient.BaseAddress = new Uri(baseUrl);
        _httpClient.Timeout = TimeSpan.FromMinutes(10);
        
        _apiKey = configuration["OpenAI:ApiKey"] 
            ?? throw new InvalidOperationException("OpenAI:ApiKey não configurado. Configure a variável de ambiente OpenAI__ApiKey ou a configuração OpenAI:ApiKey no appsettings.json");
        
        if (string.IsNullOrWhiteSpace(_apiKey) || _apiKey.Length < 20)
        {
            throw new InvalidOperationException($"OpenAI:ApiKey inválido. A chave deve ter pelo menos 20 caracteres. Valor atual: {(string.IsNullOrWhiteSpace(_apiKey) ? "(vazio)" : $"{_apiKey.Substring(0, Math.Min(10, _apiKey.Length))}...")}");
        }
        
        _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _apiKey);
        
        var organizationId = configuration["OpenAI:OrganizationId"];
        if (!string.IsNullOrWhiteSpace(organizationId))
        {
            _httpClient.DefaultRequestHeaders.Add("OpenAI-Organization", organizationId);
        }
        
        _model = configuration["OpenAI:LLM:Model"] ?? "gpt-4o-mini";
        _temperature = double.Parse(configuration["OpenAI:LLM:Temperature"] ?? "0.3");
        _maxTokens = int.Parse(configuration["OpenAI:LLM:MaxTokens"] ?? "2000");
    }

    public async Task<AudioProcessingResult> SummarizeAndExtractTasksAsync(
        string transcript,
        CancellationToken cancellationToken = default)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(transcript))
            {
                _logger.LogWarning("Transcrição vazia recebida");
                return new AudioProcessingResult
                {
                    Summary = "Transcrição vazia.",
                    Tasks = new List<ExtractedTask>()
                };
            }

            // Truncar transcrições muito longas para evitar exceder limites de tokens
            // Estimativa: ~4 caracteres por token, deixar margem para o prompt
            const int maxTranscriptLength = 50000; // ~12.5k tokens, deixando espaço para o prompt
            var processedTranscript = transcript;
            if (transcript.Length > maxTranscriptLength)
            {
                _logger.LogWarning(
                    "Transcrição muito longa ({Length} caracteres), truncando para {MaxLength} caracteres",
                    transcript.Length, maxTranscriptLength);
                processedTranscript = transcript.Substring(0, maxTranscriptLength) + "... [truncado]";
            }

            _logger.LogInformation(
                "Processando transcrição com OpenAI. Tamanho: {Length} caracteres, Model: {Model}, MaxTokens: {MaxTokens}, Temperature: {Temperature}",
                processedTranscript.Length, _model, _maxTokens, _temperature);

            var prompt = BuildPrompt(processedTranscript);

            var requestBody = new
            {
                model = _model,
                messages = new[]
                {
                    new { role = "user", content = prompt }
                },
                max_tokens = _maxTokens,
                temperature = _temperature,
                response_format = new { type = "json_object" }
            };

            var json = JsonSerializer.Serialize(requestBody);
            _logger.LogDebug("Request body size: {Size} bytes, Prompt length: {PromptLength} characters", 
                json.Length, prompt.Length);
            
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            var response = await _httpClient.PostAsync("chat/completions", content, cancellationToken);
            
            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await response.Content.ReadAsStringAsync(cancellationToken);
                
                // Tentar extrair a mensagem de erro detalhada da resposta da OpenAI
                string errorMessage = "Erro desconhecido da API OpenAI";
                try
                {
                    var errorDoc = JsonDocument.Parse(errorContent);
                    if (errorDoc.RootElement.TryGetProperty("error", out var errorProp))
                    {
                        if (errorProp.TryGetProperty("message", out var messageProp))
                        {
                            errorMessage = messageProp.GetString() ?? errorMessage;
                        }
                        else if (errorProp.ValueKind == JsonValueKind.String)
                        {
                            errorMessage = errorProp.GetString() ?? errorMessage;
                        }
                    }
                }
                catch (JsonException)
                {
                    // Se não conseguir fazer parse, usar o conteúdo completo
                    errorMessage = errorContent;
                }
                
                _logger.LogError(
                    "Erro ao processar com OpenAI LLM. Status: {StatusCode}, Model: {Model}, PromptLength: {PromptLength}, Error: {ErrorMessage}, FullResponse: {ErrorContent}",
                    response.StatusCode, _model, prompt.Length, errorMessage, errorContent);
                
                if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
                {
                    throw new UnauthorizedAccessException(
                        $"Falha de autenticação com OpenAI. Verifique se a API key está correta e válida. " +
                        $"Configure a variável de ambiente OpenAI__ApiKey ou a configuração OpenAI:ApiKey no appsettings.json. " +
                        $"Status: {response.StatusCode}, Error: {errorMessage}");
                }
                
                if (response.StatusCode == System.Net.HttpStatusCode.BadRequest)
                {
                    throw new HttpRequestException(
                        $"Requisição inválida para OpenAI API. Model: {_model}, Error: {errorMessage}. " +
                        $"Verifique se o modelo '{_model}' suporta JSON mode e se os parâmetros estão corretos. " +
                        $"Full response: {errorContent}",
                        null, response.StatusCode);
                }
                
                throw new HttpRequestException(
                    $"Erro ao processar requisição para OpenAI API. Status: {response.StatusCode}, Error: {errorMessage}. " +
                    $"Full response: {errorContent}",
                    null, response.StatusCode);
            }

            var responseJson = await response.Content.ReadAsStringAsync(cancellationToken);
            var jsonDoc = JsonDocument.Parse(responseJson);
            
            var responseText = jsonDoc.RootElement
                .GetProperty("choices")[0]
                .GetProperty("message")
                .GetProperty("content")
                .GetString() ?? string.Empty;
            
            _logger.LogDebug("Resposta do OpenAI LLM: {Response}", responseText);

            var result = ParseAudioProcessingResult(responseText, transcript);
            
            _logger.LogInformation(
                "Processamento concluído: Summary length={SummaryLength}, Tasks count={TasksCount}",
                result.Summary.Length, result.Tasks.Count);

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao processar transcrição com OpenAI LLM");
            throw;
        }
    }

    private static string BuildPrompt(string transcript)
    {
        return $@"Analise a seguinte transcrição de uma nota de áudio sobre um contato e organize as informações de forma estruturada.

TRANSCRIÇÃO:
{transcript}

Extraia e organize as informações em formato JSON válido com a seguinte estrutura:
{{
  ""summary"": ""resumo conciso em 2-3 frases do conteúdo principal"",
  ""suggestions"": [
    ""sugestão de ação 1"",
    ""sugestão de ação 2""
  ]
}}

Retorne APENAS o JSON, sem markdown, sem explicações adicionais.";
    }

    private AudioProcessingResult ParseAudioProcessingResult(string jsonText, string transcript)
    {
        try
        {
            // Limpar resposta (remover markdown code blocks se houver)
            var cleanedText = jsonText.Trim();
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

            var summary = root.TryGetProperty("summary", out var summaryProp)
                ? summaryProp.GetString()?.Trim() ?? CreateSimpleSummary(transcript)
                : CreateSimpleSummary(transcript);

            var tasks = new List<ExtractedTask>();

            // Extrair sugestões como tasks
            if (root.TryGetProperty("suggestions", out var suggestionsProp) &&
                suggestionsProp.ValueKind == JsonValueKind.Array)
            {
                foreach (var suggestion in suggestionsProp.EnumerateArray())
                {
                    var suggestionText = suggestion.GetString()?.Trim();
                    if (!string.IsNullOrWhiteSpace(suggestionText))
                    {
                        // Tentar extrair data se mencionada na sugestão
                        DateTime? dueDate = ExtractDateFromText(suggestionText);
                        
                        tasks.Add(new ExtractedTask(
                            description: suggestionText,
                            dueDate: dueDate,
                            priority: dueDate.HasValue ? "medium" : "low"));
                    }
                }
            }

            // Se não houver sugestões, criar uma genérica
            if (tasks.Count == 0)
            {
                tasks.Add(new ExtractedTask(
                    description: "Revisar nota de áudio",
                    priority: "low"));
            }

            return new AudioProcessingResult
            {
                Summary = summary,
                Tasks = tasks
            };
        }
        catch (JsonException ex)
        {
            _logger.LogError(ex, "Erro ao fazer parse do JSON da resposta OpenAI. Resposta: {Response}", jsonText);
            throw new InvalidOperationException($"Resposta inválida da OpenAI LLM: {ex.Message}", ex);
        }
    }

    private static string CreateSimpleSummary(string transcript)
    {
        if (string.IsNullOrWhiteSpace(transcript))
        {
            return "Nota de áudio processada.";
        }

        return transcript.Length > 200
            ? transcript.Substring(0, 200) + "..."
            : transcript;
    }

    private static DateTime? ExtractDateFromText(string text)
    {
        // Tentar extrair datas mencionadas no texto
        // Exemplos: "hoje", "amanhã", "próxima semana", "15/01/2024", etc.
        // Implementação simples - pode ser melhorada
        var today = DateTime.Today;
        
        if (text.Contains("hoje", StringComparison.OrdinalIgnoreCase))
        {
            return today;
        }
        
        if (text.Contains("amanhã", StringComparison.OrdinalIgnoreCase))
        {
            return today.AddDays(1);
        }
        
        if (text.Contains("próxima semana", StringComparison.OrdinalIgnoreCase))
        {
            return today.AddDays(7);
        }

        // Tentar extrair data no formato DD/MM/YYYY ou DD-MM-YYYY
        var datePattern = @"(\d{1,2})[/-](\d{1,2})[/-](\d{4})";
        var match = Regex.Match(text, datePattern);
        if (match.Success)
        {
            try
            {
                var day = int.Parse(match.Groups[1].Value);
                var month = int.Parse(match.Groups[2].Value);
                var year = int.Parse(match.Groups[3].Value);
                return new DateTime(year, month, day);
            }
            catch
            {
                // Ignorar erros de parsing
            }
        }

        return null;
    }
}
