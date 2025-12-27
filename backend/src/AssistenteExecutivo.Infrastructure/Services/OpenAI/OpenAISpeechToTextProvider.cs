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
            // Limite da API OpenAI Whisper: 25 MB por requisição
            // Nota: O AudioTrimmer no controller deveria ter cortado arquivos maiores antes de chegar aqui
            const long maxFileSizeBytes = 25 * 1024 * 1024; // 25 MB
            
            if (audioBytes.Length > maxFileSizeBytes)
            {
                var fileSizeMB = audioBytes.Length / (1024.0 * 1024.0);
                var errorMessage = $"Arquivo de áudio muito grande ({fileSizeMB:F2} MB) chegou ao provider de transcrição. " +
                    $"O AudioTrimmer deveria ter cortado o arquivo antes. " +
                    $"A API OpenAI Whisper aceita arquivos de até 25 MB por requisição.";
                
                _logger.LogError(
                    "Arquivo de áudio excede o limite da API (não deveria chegar aqui após AudioTrimmer). Tamanho: {Size} bytes ({SizeMB:F2} MB), Limite: {MaxSize} bytes (25 MB)",
                    audioBytes.Length, fileSizeMB, maxFileSizeBytes);
                
                throw new ArgumentException(errorMessage, nameof(audioBytes));
            }

            _logger.LogInformation(
                "Iniciando transcrição de áudio com OpenAI Whisper. Tamanho: {Size} bytes ({SizeMB:F2} MB), MimeType: {MimeType}, Model: {Model}, Language: {Language}",
                audioBytes.Length, audioBytes.Length / (1024.0 * 1024.0), mimeType, _model, _language);

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
                    "Erro ao transcrever áudio com OpenAI. Status: {StatusCode}, FileSize: {FileSize} bytes ({FileSizeMB:F2} MB), Error: {ErrorMessage}, FullResponse: {ErrorContent}",
                    response.StatusCode, audioBytes.Length, audioBytes.Length / (1024.0 * 1024.0), errorMessage, errorContent);
                
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
                        $"Requisição inválida para OpenAI Whisper API. FileSize: {audioBytes.Length / (1024.0 * 1024.0):F2} MB, Error: {errorMessage}. " +
                        $"Verifique se o arquivo está em um formato suportado (mp3, mp4, mpeg, mpga, m4a, wav, webm) e se o tamanho não excede 25 MB. " +
                        $"Full response: {errorContent}",
                        null, response.StatusCode);
                }
                
                throw new HttpRequestException(
                    $"Erro ao processar requisição para OpenAI Whisper API. Status: {response.StatusCode}, Error: {errorMessage}. " +
                    $"Full response: {errorContent}",
                    null, response.StatusCode);
            }

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
