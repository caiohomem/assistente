using AssistenteExecutivo.Domain.Interfaces;
using AssistenteExecutivo.Domain.ValueObjects;

namespace AssistenteExecutivo.Infrastructure.Services;

/// <summary>
/// Stub implementation of ISpeechToTextProvider.
/// This is a placeholder that returns empty transcripts.
/// Replace with actual implementation (e.g., Azure Cognitive Services, Google Cloud Speech-to-Text) when ready.
/// </summary>
public class StubSpeechToTextProvider : ISpeechToTextProvider
{
    public Task<Transcript> TranscribeAsync(byte[] audioBytes, string mimeType, CancellationToken cancellationToken = default)
    {
        // Stub implementation: returns empty transcript
        // TODO: Replace with actual speech-to-text service integration
        return Task.FromResult(new Transcript(""));
    }
}












