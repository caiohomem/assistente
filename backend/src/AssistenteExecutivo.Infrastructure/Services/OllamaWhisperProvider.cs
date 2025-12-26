using AssistenteExecutivo.Domain.Interfaces;
using AssistenteExecutivo.Domain.ValueObjects;
using AssistenteExecutivo.Infrastructure.HttpClients;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.Net.Http.Json;

namespace AssistenteExecutivo.Infrastructure.Services;

/// <summary>
/// Provider de transcrição de áudio usando Whisper via Ollama
/// Nota: O Ollama pode ter suporte para Whisper dependendo da versão.
/// Se não houver, este provider pode ser adaptado para usar uma API externa.
/// </summary>
public class OllamaWhisperProvider : ISpeechToTextProvider
{
    private readonly OllamaClient? _ollamaClient;
    private readonly ILogger<OllamaWhisperProvider> _logger;
    private readonly string _model;
    private readonly string? _apiUrl;
    private readonly HttpClient? _httpClient;
    private readonly IHttpClientFactory? _httpClientFactory;

    public OllamaWhisperProvider(
        IConfiguration configuration,
        ILogger<OllamaWhisperProvider> logger,
        OllamaClient? ollamaClient = null,
        HttpClient? httpClient = null,
        IHttpClientFactory? httpClientFactory = null)
    {
        _logger = logger;
        _ollamaClient = ollamaClient;
        _httpClient = httpClient;
        _httpClientFactory = httpClientFactory;
        _model = configuration["Whisper:Model"] ?? "whisper";
        _apiUrl = configuration["Whisper:ApiUrl"];
    }

    public async Task<Transcript> TranscribeAsync(
        byte[] audioBytes,
        string mimeType,
        CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Iniciando transcrição de áudio. Tamanho: {Size} bytes, MimeType: {MimeType}", 
                audioBytes.Length, mimeType);

            // Se houver API URL configurada, usar API externa (faster-whisper-server, etc)
            if (!string.IsNullOrWhiteSpace(_apiUrl))
            {
                return await TranscribeViaApiAsync(audioBytes, mimeType, cancellationToken);
            }

            // Tentar usar Ollama diretamente (se suportar Whisper)
            return await TranscribeViaOllamaAsync(audioBytes, mimeType, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao transcrever áudio");
            throw;
        }
    }

    private async Task<Transcript> TranscribeViaOllamaAsync(
        byte[] audioBytes,
        string mimeType,
        CancellationToken cancellationToken)
    {
        if (_ollamaClient == null)
        {
            throw new InvalidOperationException("OllamaClient não está disponível. Configure Whisper:ApiUrl para usar API externa.");
        }

        // Nota: O Ollama atualmente não suporta áudio diretamente via API padrão
        // Este método é um placeholder para futura implementação
        // Por enquanto, vamos usar API externa se configurada
        if (!string.IsNullOrWhiteSpace(_apiUrl))
        {
            return await TranscribeViaApiAsync(audioBytes, mimeType, cancellationToken);
        }

        throw new NotSupportedException(
            "Transcrição via Ollama diretamente não é suportada ainda. " +
            "Configure Whisper:ApiUrl para usar uma API externa de transcrição (ex: faster-whisper-server).");
    }

    private async Task<Transcript> TranscribeViaApiAsync(
        byte[] audioBytes,
        string mimeType,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(_apiUrl))
        {
            throw new InvalidOperationException("Whisper:ApiUrl não configurado. Configure uma URL de API de transcrição (ex: faster-whisper-server).");
        }

        HttpClient? clientToUse = _httpClient;
        
        if (clientToUse == null && _httpClientFactory != null)
        {
            // Tentar criar via factory se disponível
            clientToUse = _httpClientFactory.CreateClient();
            if (clientToUse != null && !string.IsNullOrWhiteSpace(_apiUrl))
            {
                clientToUse.BaseAddress = new Uri(_apiUrl);
                clientToUse.Timeout = TimeSpan.FromMinutes(10);
            }
        }
        
        if (clientToUse == null)
        {
            // Criar HttpClient temporário se não foi injetado
            using var tempClient = new HttpClient();
            tempClient.BaseAddress = new Uri(_apiUrl!);
            tempClient.Timeout = TimeSpan.FromMinutes(10);
            return await TranscribeWithClientAsync(tempClient, audioBytes, mimeType, cancellationToken);
        }

        _logger.LogInformation("Usando API externa para transcrição: {ApiUrl}", _apiUrl);
        return await TranscribeWithClientAsync(clientToUse, audioBytes, mimeType, cancellationToken);
    }

    private async Task<Transcript> TranscribeWithClientAsync(
        HttpClient client,
        byte[] audioBytes,
        string mimeType,
        CancellationToken cancellationToken)
    {
        // Criar multipart form data
        using var content = new MultipartFormDataContent();
        var audioContent = new ByteArrayContent(audioBytes);
        audioContent.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue(mimeType);
        content.Add(audioContent, "file", "audio.wav");

        var language = "pt"; // Português
        var apiUrl = _apiUrl ?? client.BaseAddress?.ToString() ?? throw new InvalidOperationException("API URL não configurada");
        var endpoint = apiUrl.EndsWith("/") ? $"{apiUrl}transcribe" : $"{apiUrl}/transcribe";

        var response = await client.PostAsync(
            $"{endpoint}?language={language}",
            content,
            cancellationToken);

        response.EnsureSuccessStatusCode();

        var result = await response.Content.ReadFromJsonAsync<WhisperApiResponse>(
            cancellationToken: cancellationToken);

        if (result == null)
        {
            throw new InvalidOperationException("Resposta inválida da API de transcrição");
        }

        // Verificar se há erro na resposta
        if (!string.IsNullOrWhiteSpace(result.Error))
        {
            throw new InvalidOperationException($"Erro na API de transcrição: {result.Error}");
        }

        if (string.IsNullOrWhiteSpace(result.Text))
        {
            throw new InvalidOperationException("Resposta vazia da API de transcrição");
        }

        var transcriptText = result.Text.Trim();
        
        _logger.LogInformation("Transcrição concluída via API. Tamanho do texto: {Length} caracteres, Idioma: {Language}, Duração: {Duration}s", 
            transcriptText.Length, result.Language, result.Duration);

        // FastAPI retorna apenas text, language e duration (sem segments)
        // Criar transcript simples sem segmentos
        return new Transcript(transcriptText);
    }

    private class WhisperApiResponse
    {
        public string Text { get; set; } = string.Empty;
        public string Language { get; set; } = string.Empty;
        public double Duration { get; set; }
        // FastAPI pode retornar error em caso de falha
        public string? Error { get; set; }
    }
}

