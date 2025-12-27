using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using AssistenteExecutivo.Domain.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace AssistenteExecutivo.Infrastructure.Services.OpenAI;

public sealed class OpenAITextToSpeechProvider : ITextToSpeechProvider
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<OpenAITextToSpeechProvider> _logger;
    private readonly string _model;
    private readonly string _defaultVoice;
    private readonly string _defaultFormat;
    private readonly string _apiKey;

    public OpenAITextToSpeechProvider(
        IConfiguration configuration,
        IHttpClientFactory httpClientFactory,
        ILogger<OpenAITextToSpeechProvider> logger)
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
        
        _model = configuration["OpenAI:TextToSpeech:Model"] ?? "tts-1";
        _defaultVoice = configuration["OpenAI:TextToSpeech:Voice"] ?? "nova";
        _defaultFormat = configuration["OpenAI:TextToSpeech:Format"] ?? "mp3";
    }

    public async Task<byte[]> SynthesizeAsync(
        string text,
        string voice,
        string format,
        CancellationToken cancellationToken = default)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(text))
            {
                _logger.LogWarning("Texto vazio recebido para síntese de voz");
                return Array.Empty<byte>();
            }

            var voiceToUse = string.IsNullOrWhiteSpace(voice) ? _defaultVoice : voice;
            var formatToUse = string.IsNullOrWhiteSpace(format) ? _defaultFormat : format;

            _logger.LogInformation(
                "Sintetizando texto em áudio. Tamanho do texto: {Length} caracteres, Voz: {Voice}, Formato: {Format}, Model: {Model}",
                text.Length, voiceToUse, formatToUse, _model);

            var requestBody = new
            {
                model = _model,
                input = text,
                voice = voiceToUse,
                response_format = formatToUse
            };

            var json = JsonSerializer.Serialize(requestBody);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            var response = await _httpClient.PostAsync("audio/speech", content, cancellationToken);
            response.EnsureSuccessStatusCode();

            var audioBytes = await response.Content.ReadAsByteArrayAsync(cancellationToken);

            _logger.LogInformation(
                "Síntese de voz concluída. Tamanho do áudio gerado: {Size} bytes",
                audioBytes.Length);

            return audioBytes;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao sintetizar texto em áudio com OpenAI TTS");
            throw;
        }
    }
}
