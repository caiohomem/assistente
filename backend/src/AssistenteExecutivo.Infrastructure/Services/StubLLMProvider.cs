using AssistenteExecutivo.Domain.Interfaces;
using AssistenteExecutivo.Domain.ValueObjects;

namespace AssistenteExecutivo.Infrastructure.Services;

/// <summary>
/// Stub implementation of ILLMProvider.
/// This is a placeholder that returns empty results.
/// Replace with actual implementation (e.g., OpenAI, Azure OpenAI, Anthropic Claude) when ready.
/// </summary>
public class StubLLMProvider : ILLMProvider
{
    public Task<AudioProcessingResult> SummarizeAndExtractTasksAsync(string transcript, CancellationToken cancellationToken = default)
    {
        // Stub implementation: returns a basic summary based on transcript
        // TODO: Replace with actual LLM service integration
        var summary = string.IsNullOrWhiteSpace(transcript)
            ? "Nota de áudio processada (sem transcrição disponível)."
            : transcript.Length > 200
                ? transcript.Substring(0, 200) + "..."
                : transcript;

        return Task.FromResult(new AudioProcessingResult
        {
            Summary = summary,
            Tasks = new List<ExtractedTask>()
        });
    }
}

