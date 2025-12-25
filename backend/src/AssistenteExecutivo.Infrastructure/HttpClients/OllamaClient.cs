using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Logging;

namespace AssistenteExecutivo.Infrastructure.HttpClients;

public class OllamaClient
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<OllamaClient> _logger;
    private readonly string _baseUrl;

    public OllamaClient(HttpClient httpClient, ILogger<OllamaClient> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
        _baseUrl = httpClient.BaseAddress?.ToString().TrimEnd('/') ?? "http://localhost:11434";
        _logger.LogInformation("OllamaClient inicializado com BaseUrl: {BaseUrl}", _baseUrl);
    }

    /// <summary>
    /// Gera resposta de texto usando o modelo especificado
    /// Para modelos de visão, use ChatAsync com imagens
    /// </summary>
    public async Task<string> GenerateAsync(
        string model,
        string prompt,
        string? imageBase64 = null,
        double temperature = 0.7,
        int maxTokens = 500,
        CancellationToken cancellationToken = default)
    {
        // Se houver imagem, usar chat endpoint (para modelos de visão como LLaVA)
        if (imageBase64 != null)
        {
            return await ChatAsync(model, prompt, imageBase64, temperature, maxTokens, cancellationToken);
        }

        var request = new
        {
            model = model,
            prompt = prompt,
            stream = false,
            options = new
            {
                temperature = temperature,
                num_predict = maxTokens
            }
        };

        var response = await _httpClient.PostAsJsonAsync(
            $"{_baseUrl}/api/generate",
            request,
            cancellationToken);

        response.EnsureSuccessStatusCode();

        var result = await response.Content.ReadFromJsonAsync<OllamaGenerateResponse>(
            cancellationToken: cancellationToken);

        if (result == null || string.IsNullOrWhiteSpace(result.Response))
        {
            throw new InvalidOperationException("Resposta vazia do Ollama");
        }

        return result.Response;
    }

    /// <summary>
    /// Chat com o modelo (formato de conversa)
    /// Suporta imagens para modelos de visão (LLaVA, etc)
    /// </summary>
    public async Task<string> ChatAsync(
        string model,
        string message,
        string? imageBase64 = null,
        double temperature = 0.7,
        int maxTokens = 2000,
        CancellationToken cancellationToken = default)
    {
        // Para Ollama com modelos de visão, o formato correto é passar imagens
        // como um campo separado no nível raiz da requisição
        object request;
        
        if (imageBase64 != null)
        {
            // Formato para modelos de visão: imagens como array no nível raiz
            request = new
            {
                model = model,
                messages = new[]
                {
                    new
                    {
                        role = "user",
                        content = message,
                        images = new[] { imageBase64 }
                    }
                },
                images = new[] { imageBase64 },
                stream = false,
                options = new
                {
                    temperature = temperature,
                    num_predict = maxTokens
                }
            };
        }
        else
        {
            // Formato padrão sem imagens
            request = new
            {
                model = model,
                messages = new[]
                {
                    new
                    {
                        role = "user",
                        content = message
                    }
                },
                stream = false,
                options = new
                {
                    temperature = temperature,
                    num_predict = maxTokens
                }
            };
        }

        // Construir URL completa - usar URI relativo se BaseAddress estiver configurado
        var url = _httpClient.BaseAddress != null 
            ? new Uri(_httpClient.BaseAddress, "/api/chat")
            : new Uri($"{_baseUrl}/api/chat");
        
        _logger.LogDebug("Enviando requisição para Ollama. URL: {Url}, Model: {Model}, HasImage: {HasImage}", 
            url, model, imageBase64 != null);

        var response = await _httpClient.PostAsJsonAsync(
            url,
            request,
            cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            var errorContent = await response.Content.ReadAsStringAsync(cancellationToken);
            _logger.LogError("Erro na chamada ao Ollama. Status: {Status}, Response: {Response}, URL: {Url}", 
                response.StatusCode, errorContent, $"{_baseUrl}/api/chat");
            response.EnsureSuccessStatusCode();
        }

        var result = await response.Content.ReadFromJsonAsync<OllamaChatResponse>(
            cancellationToken: cancellationToken);

        if (result == null)
        {
            var rawContent = await response.Content.ReadAsStringAsync(cancellationToken);
            _logger.LogError("Resposta do Ollama é null. Raw content: {Content}", rawContent);
            throw new InvalidOperationException($"Resposta do Ollama é null. Raw: {rawContent}");
        }

        // Verificar se há resposta direta (formato generate)
        if (!string.IsNullOrWhiteSpace(result.Response))
        {
            _logger.LogDebug("Usando Response direto do Ollama. Tamanho: {Length} chars", result.Response.Length);
            return result.Response;
        }

        if (result.Message == null)
        {
            _logger.LogError("Message é null na resposta do Ollama. Response: {Response}", 
                System.Text.Json.JsonSerializer.Serialize(result));
            throw new InvalidOperationException("Message é null na resposta do Ollama");
        }

        if (string.IsNullOrWhiteSpace(result.Message.Content))
        {
            _logger.LogWarning("Content vazio na resposta do Ollama. Message: {Message}, Done: {Done}, DoneReason: {DoneReason}", 
                System.Text.Json.JsonSerializer.Serialize(result.Message), result.Done, 
                result.GetType().GetProperty("DoneReason")?.GetValue(result)?.ToString() ?? "unknown");
            
            // Tentar usar a resposta completa como fallback
            var rawContent = await response.Content.ReadAsStringAsync(cancellationToken);
            _logger.LogInformation("Raw response completo: {Raw}", rawContent);
            
            // Se o modelo retornou vazio, pode ser problema com o prompt ou modelo
            throw new InvalidOperationException(
                $"Content vazio na resposta do Ollama. O modelo pode não ter conseguido processar a imagem ou o prompt. " +
                $"Tente outro modelo ou ajuste o prompt. Raw response: {rawContent}");
        }

        _logger.LogDebug("Resposta do Ollama recebida com sucesso. Tamanho: {Length} chars", 
            result.Message.Content.Length);

        return result.Message.Content;
    }

    /// <summary>
    /// Verifica se o modelo está disponível
    /// </summary>
    public async Task<bool> IsModelAvailableAsync(string model, CancellationToken cancellationToken = default)
    {
        try
        {
            var response = await _httpClient.GetAsync($"{_baseUrl}/api/tags", cancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                return false;
            }

            var result = await response.Content.ReadFromJsonAsync<OllamaTagsResponse>(
                cancellationToken: cancellationToken);

            return result?.Models?.Any(m => m.Name == model || m.Name.StartsWith($"{model}:")) ?? false;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Erro ao verificar disponibilidade do modelo {Model}", model);
            return false;
        }
    }

    private class OllamaGenerateResponse
    {
        [JsonPropertyName("response")]
        public string Response { get; set; } = string.Empty;

        [JsonPropertyName("done")]
        public bool Done { get; set; }
    }

    private class OllamaChatResponse
    {
        [JsonPropertyName("message")]
        public OllamaMessage? Message { get; set; }

        [JsonPropertyName("done")]
        public bool Done { get; set; }
        
        [JsonPropertyName("response")]
        public string? Response { get; set; }
        
        [JsonPropertyName("done_reason")]
        public string? DoneReason { get; set; }
        
        [JsonPropertyName("eval_count")]
        public int? EvalCount { get; set; }
    }

    private class OllamaMessage
    {
        [JsonPropertyName("content")]
        public string Content { get; set; } = string.Empty;
        
        [JsonPropertyName("role")]
        public string? Role { get; set; }
    }

    private class OllamaTagsResponse
    {
        [JsonPropertyName("models")]
        public List<OllamaModel>? Models { get; set; }
    }

    private class OllamaModel
    {
        [JsonPropertyName("name")]
        public string Name { get; set; } = string.Empty;
    }
}

