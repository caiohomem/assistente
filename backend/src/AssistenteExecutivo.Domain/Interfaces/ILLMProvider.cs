using AssistenteExecutivo.Domain.ValueObjects;

namespace AssistenteExecutivo.Domain.Interfaces;

public class AudioProcessingResult
{
    public string Summary { get; set; } = string.Empty;
    public List<ExtractedTask> Tasks { get; set; } = new();
}

public interface ILLMProvider
{
    Task<AudioProcessingResult> SummarizeAndExtractTasksAsync(string transcript, CancellationToken cancellationToken = default);
}












