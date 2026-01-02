using AssistenteExecutivo.Domain.ValueObjects;

namespace AssistenteExecutivo.Domain.Interfaces;

public interface ISpeechToTextProvider
{
    Task<Transcript> TranscribeAsync(byte[] audioBytes, string mimeType, CancellationToken cancellationToken = default);
}














