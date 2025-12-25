using System.Net.Http.Json;
using System.Text.Json.Serialization;

namespace AssistenteExecutivo.Infrastructure.HttpClients;

public sealed class PaddleOcrApiClient
{
    private readonly HttpClient _httpClient;

    public PaddleOcrApiClient(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task<PaddleOcrResponse> OcrAsync(
        byte[] imageBytes,
        string fileName,
        string mimeType,
        string lang,
        CancellationToken cancellationToken)
    {
        using var form = new MultipartFormDataContent();
        using var imageContent = new ByteArrayContent(imageBytes);
        imageContent.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue(mimeType);
        form.Add(imageContent, "file", fileName);

        var url = $"/ocr?lang={Uri.EscapeDataString(lang)}";
        var response = await _httpClient.PostAsync(url, form, cancellationToken);
        response.EnsureSuccessStatusCode();

        var payload = await response.Content.ReadFromJsonAsync<PaddleOcrResponse>(cancellationToken: cancellationToken);
        return payload ?? new PaddleOcrResponse();
    }

    public sealed class PaddleOcrResponse
    {
        [JsonPropertyName("rawText")]
        public string? RawText { get; set; }

        [JsonPropertyName("lines")]
        public List<PaddleOcrLine>? Lines { get; set; }

        [JsonPropertyName("lang")]
        public string? Lang { get; set; }
    }

    public sealed class PaddleOcrLine
    {
        [JsonPropertyName("text")]
        public string? Text { get; set; }

        [JsonPropertyName("confidence")]
        public double Confidence { get; set; }
    }
}

