using System.Net.Http.Headers;
using System.Text.Json;
using AssistenteExecutivo.Domain.Interfaces;
using AssistenteExecutivo.Domain.ValueObjects;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace AssistenteExecutivo.Infrastructure.Services.OpenAI;

public sealed class OpenAISpeechToTextProvider : ISpeechToTextProvider
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<OpenAISpeechToTextProvider> _logger;
    private readonly string _model;
    private readonly string _language;
    private readonly string _apiKey;

    public OpenAISpeechToTextProvider(
        IConfiguration configuration,
        IHttpClientFactory httpClientFactory,
        ILogger<OpenAISpeechToTextProvider> logger)
    {
        _logger = logger;
        _httpClient = httpClientFactory.CreateClient();
        _httpClient.BaseAddress = new Uri("https://api.openai.com/v1/");
        _httpClient.Timeout = TimeSpan.FromMinutes(10);
        
        _apiKey = configuration["OpenAI:ApiKey"] 
            ?? throw new InvalidOperationException("OpenAI:ApiKey não configurado");
        
        _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _apiKey);
        
        var organizationId = configuration["OpenAI:OrganizationId"];
        if (!string.IsNullOrWhiteSpace(organizationId))
        {
            _httpClient.DefaultRequestHeaders.Add("OpenAI-Organization", organizationId);
        }
        
        _model = configuration["OpenAI:SpeechToText:Model"] ?? "whisper-1";
        _language = configuration["OpenAI:SpeechToText:Language"] ?? "pt";
    }

    public async Task<Transcript> TranscribeAsync(
        byte[] audioBytes,
        string mimeType,
        CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation(
                "Iniciando transcrição de áudio com OpenAI Whisper. Tamanho: {Size} bytes, MimeType: {MimeType}, Model: {Model}, Language: {Language}",
                audioBytes.Length, mimeType, _model, _language);

            using var content = new MultipartFormDataContent();
            
            // Adicionar arquivo de áudio
            var audioContent = new ByteArrayContent(audioBytes);
            audioContent.Headers.ContentType = new MediaTypeHeaderValue(mimeType);
            content.Add(audioContent, "file", "audio.wav");
            
            // Adicionar parâmetros
            content.Add(new StringContent(_model), "model");
            content.Add(new StringContent(_language), "language");
            content.Add(new StringContent("json"), "response_format");

            var response = await _httpClient.PostAsync("audio/transcriptions", content, cancellationToken);
            response.EnsureSuccessStatusCode();

            var responseJson = await response.Content.ReadAsStringAsync(cancellationToken);
            var jsonDoc = JsonDocument.Parse(responseJson);
            
            var transcriptText = jsonDoc.RootElement.TryGetProperty("text", out var textProp)
                ? textProp.GetString()?.Trim() ?? string.Empty
                : string.Empty;
            
            var detectedLanguage = jsonDoc.RootElement.TryGetProperty("language", out var langProp)
                ? langProp.GetString()
                : _language;
            
            _logger.LogInformation(
                "Transcrição concluída. Tamanho do texto: {Length} caracteres, Idioma detectado: {Language}",
                transcriptText.Length, detectedLanguage);

            return new Transcript(transcriptText);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao transcrever áudio com OpenAI Whisper");
            throw;
        }
    }
}
