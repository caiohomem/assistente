using AssistenteExecutivo.Domain.Interfaces;

namespace AssistenteExecutivo.Infrastructure.Services;

/// <summary>
/// Stub implementation of ITextToSpeechProvider.
/// This is a placeholder that returns empty audio bytes.
/// Replace with actual implementation (e.g., OpenAI TTS) when ready.
/// </summary>
public class StubTextToSpeechProvider : ITextToSpeechProvider
{
    public Task<byte[]> SynthesizeAsync(
        string text,
        string voice,
        string format,
        CancellationToken cancellationToken = default)
    {
        // Stub implementation: returns empty audio bytes
        // TODO: Replace with actual TTS service integration
        return Task.FromResult(Array.Empty<byte>());
    }
}










