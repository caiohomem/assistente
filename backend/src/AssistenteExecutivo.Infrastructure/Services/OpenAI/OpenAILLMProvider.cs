using AssistenteExecutivo.Application.Interfaces;
using AssistenteExecutivo.Domain.Interfaces;
using AssistenteExecutivo.Domain.ValueObjects;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace AssistenteExecutivo.Infrastructure.Services.OpenAI;

public sealed class OpenAILLMProvider : ILLMProvider
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<OpenAILLMProvider> _logger;
    private readonly IAgentConfigurationRepository _configurationRepository;
    private readonly string _model;
    private readonly double _temperature;
    private readonly int _maxTokens;
    private readonly string _apiKey;

    public OpenAILLMProvider(
        IConfiguration configuration,
        IHttpClientFactory httpClientFactory,
        ILogger<OpenAILLMProvider> logger,
        IAgentConfigurationRepository configurationRepository)
    {
        _logger = logger;
        _configurationRepository = configurationRepository;
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
        var temperatureValue = double.Parse(configuration["OpenAI:LLM:Temperature"] ?? "0.3");
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

            var prompt = await BuildPromptAsync(processedTranscript, cancellationToken);

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

            // Ler resposta de forma mais robusta usando stream para garantir leitura completa
            string responseJson;
            try
            {
                using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
                using var reader = new StreamReader(stream, Encoding.UTF8, detectEncodingFromByteOrderMarks: true, bufferSize: 8192, leaveOpen: false);
                responseJson = await reader.ReadToEndAsync(cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao ler resposta da API OpenAI");
                throw new HttpRequestException($"Erro ao ler resposta da API OpenAI: {ex.Message}", ex);
            }

            // Validar que a resposta não está vazia ou truncada
            if (string.IsNullOrWhiteSpace(responseJson))
            {
                _logger.LogError("Resposta vazia da API OpenAI");
                throw new HttpRequestException("Resposta vazia da API OpenAI");
            }

            _logger.LogDebug("Resposta completa da API OpenAI: Tamanho={Size} bytes, Preview={Preview}",
                responseJson.Length,
                responseJson.Length > 500 ? responseJson.Substring(0, 500) + "..." : responseJson);

            JsonDocument jsonDoc;
            try
            {
                jsonDoc = JsonDocument.Parse(responseJson);
            }
            catch (JsonException ex)
            {
                _logger.LogError(ex,
                    "Erro ao fazer parse da resposta JSON da API OpenAI. Tamanho: {Size} bytes, Conteúdo: {Content}",
                    responseJson.Length,
                    responseJson.Length > 2000 ? responseJson.Substring(0, 2000) + "..." : responseJson);
                throw new HttpRequestException(
                    $"Resposta JSON inválida da API OpenAI (possivelmente truncada). Tamanho: {responseJson.Length} bytes. Erro: {ex.Message}",
                    ex);
            }

            string responseText;
            try
            {
                using (jsonDoc)
                {
                    if (!jsonDoc.RootElement.TryGetProperty("choices", out var choicesProp) ||
                        choicesProp.ValueKind != JsonValueKind.Array)
                    {
                        _logger.LogError(
                            "Resposta da API OpenAI não contém 'choices' ou não é um array. Resposta: {Response}",
                            responseJson.Length > 2000 ? responseJson.Substring(0, 2000) + "..." : responseJson);
                        throw new HttpRequestException("Resposta da API OpenAI não contém 'choices' ou não é um array");
                    }

                    // Verificar se o array não está vazio
                    var choicesArray = choicesProp.EnumerateArray();
                    if (!choicesArray.MoveNext())
                    {
                        _logger.LogError(
                            "Resposta da API OpenAI contém array 'choices' vazio. Resposta: {Response}",
                            responseJson.Length > 2000 ? responseJson.Substring(0, 2000) + "..." : responseJson);
                        throw new HttpRequestException("Resposta da API OpenAI contém array 'choices' vazio");
                    }

                    var firstChoice = choicesArray.Current;
                    if (!firstChoice.TryGetProperty("message", out var messageProp))
                    {
                        _logger.LogError(
                            "Resposta da API OpenAI não contém 'message' no choice. Resposta: {Response}",
                            responseJson.Length > 2000 ? responseJson.Substring(0, 2000) + "..." : responseJson);
                        throw new HttpRequestException("Resposta da API OpenAI não contém 'message' no choice");
                    }

                    if (!messageProp.TryGetProperty("content", out var contentProp))
                    {
                        _logger.LogError(
                            "Resposta da API OpenAI não contém 'content' na mensagem. Resposta: {Response}",
                            responseJson.Length > 2000 ? responseJson.Substring(0, 2000) + "..." : responseJson);
                        throw new HttpRequestException("Resposta da API OpenAI não contém 'content' na mensagem");
                    }

                    responseText = contentProp.GetString() ?? string.Empty;
                }
            }
            catch (HttpRequestException)
            {
                // Re-throw HttpRequestException
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "Erro ao extrair conteúdo da resposta da API OpenAI. Resposta: {Response}",
                    responseJson.Length > 2000 ? responseJson.Substring(0, 2000) + "..." : responseJson);
                throw new HttpRequestException(
                    $"Erro ao extrair conteúdo da resposta da API OpenAI: {ex.Message}",
                    ex);
            }

            if (string.IsNullOrWhiteSpace(responseText))
            {
                _logger.LogWarning("Conteúdo vazio na resposta da API OpenAI. Usando fallback.");
                return new AudioProcessingResult
                {
                    Summary = CreateSimpleSummary(transcript),
                    Tasks = new List<ExtractedTask>
                    {
                        new ExtractedTask("Revisar nota de áudio", priority: "low")
                    }
                };
            }

            _logger.LogDebug("Resposta do OpenAI LLM: Tamanho={Size} caracteres, Preview={Preview}",
                responseText.Length,
                responseText.Length > 500 ? responseText.Substring(0, 500) + "..." : responseText);

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

    private async Task<string> BuildPromptAsync(string transcript, CancellationToken cancellationToken)
    {
        // Tentar obter o prompt de transcrição da base de dados
        var configuration = await _configurationRepository.GetCurrentAsync(cancellationToken);

        string transcriptionPrompt;
        if (configuration != null && !string.IsNullOrWhiteSpace(configuration.TranscriptionPrompt))
        {
            transcriptionPrompt = configuration.TranscriptionPrompt;
            _logger.LogDebug("Usando prompt de transcrição da base de dados");
        }
        else
        {
            // Fallback para o prompt padrão
            transcriptionPrompt = GetDefaultTranscriptionPrompt();
            _logger.LogDebug("Usando prompt de transcrição padrão (configuração não encontrada na base de dados)");
        }

        // Adicionar a transcrição no final do prompt
        return $@"{transcriptionPrompt}

TRANSCRIÇÃO:
{transcript}";
    }

    private static string GetDefaultTranscriptionPrompt()
    {
        return @"Analise a seguinte transcrição de uma nota de áudio sobre um contato e organize as informações de forma estruturada.

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

            // Validar que o JSON não está vazio
            if (string.IsNullOrWhiteSpace(cleanedText))
            {
                _logger.LogWarning("JSON vazio após limpeza. Usando fallback.");
                return new AudioProcessingResult
                {
                    Summary = CreateSimpleSummary(transcript),
                    Tasks = new List<ExtractedTask>
                    {
                        new ExtractedTask("Revisar nota de áudio", priority: "low")
                    }
                };
            }

            // Validar que o JSON parece completo (verificar balanceamento básico)
            var openBraces = cleanedText.Count(c => c == '{');
            var closeBraces = cleanedText.Count(c => c == '}');
            var openBrackets = cleanedText.Count(c => c == '[');
            var closeBrackets = cleanedText.Count(c => c == ']');

            if (openBraces != closeBraces || openBrackets != closeBrackets)
            {
                _logger.LogWarning(
                    "JSON parece incompleto ou malformado. Chaves: {OpenBraces}/{CloseBraces}, Colchetes: {OpenBrackets}/{CloseBrackets}. " +
                    "Tamanho: {Size} caracteres. Preview: {Preview}",
                    openBraces, closeBraces, openBrackets, closeBrackets,
                    cleanedText.Length,
                    cleanedText.Length > 1000 ? cleanedText.Substring(0, 1000) + "..." : cleanedText);

                // Tentar corrigir JSON incompleto adicionando fechamentos faltantes
                var fixedText = cleanedText;
                if (openBraces > closeBraces)
                {
                    fixedText += new string('}', openBraces - closeBraces);
                }
                if (openBrackets > closeBrackets)
                {
                    fixedText += new string(']', openBrackets - closeBrackets);
                }

                _logger.LogInformation("Tentando corrigir JSON adicionando fechamentos faltantes");
                cleanedText = fixedText;
            }

            _logger.LogDebug("Tentando fazer parse do JSON. Tamanho: {Size} caracteres", cleanedText.Length);

            JsonDocument doc;
            try
            {
                doc = JsonDocument.Parse(cleanedText);
            }
            catch (JsonException parseEx)
            {
                _logger.LogError(parseEx,
                    "Erro ao fazer parse do JSON após limpeza. Tamanho: {Size} caracteres, " +
                    "Preview (primeiros 2000 chars): {Preview}",
                    cleanedText.Length,
                    cleanedText.Length > 2000 ? cleanedText.Substring(0, 2000) + "..." : cleanedText);

                // Se o JSON está muito corrompido, retornar resultado básico
                return new AudioProcessingResult
                {
                    Summary = CreateSimpleSummary(transcript),
                    Tasks = new List<ExtractedTask>
                    {
                        new ExtractedTask("Revisar nota de áudio", priority: "low")
                    }
                };
            }

            using (doc)
            {
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
        }
        catch (Exception ex) when (!(ex is JsonException))
        {
            // Capturar qualquer outro tipo de exceção (não apenas JsonException)
            _logger.LogError(ex,
                "Erro inesperado ao processar resposta da OpenAI LLM. Resposta original: {Response}",
                jsonText?.Length > 2000 ? jsonText.Substring(0, 2000) + "..." : jsonText ?? "(null)");

            // Retornar resultado básico em vez de lançar exceção
            return new AudioProcessingResult
            {
                Summary = CreateSimpleSummary(transcript),
                Tasks = new List<ExtractedTask>
                {
                    new ExtractedTask("Revisar nota de áudio", priority: "low")
                }
            };
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
