using AssistenteExecutivo.Domain.Enums;

namespace AssistenteExecutivo.Application.DTOs;

public class CaptureJobDto
{
    public Guid JobId { get; set; }
    public Guid OwnerUserId { get; set; }
    public JobType Type { get; set; }
    public Guid? ContactId { get; set; }
    public Guid MediaId { get; set; }
    public JobStatus Status { get; set; }
    public DateTime RequestedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
    public string? ErrorCode { get; set; }
    public string? ErrorMessage { get; set; }

    // Resultados específicos por tipo
    public CardScanResultDto? CardScanResult { get; set; }
    public AudioTranscriptDto? AudioTranscript { get; set; }
    public string? AudioSummary { get; set; }
    public List<ExtractedTaskDto>? ExtractedTasks { get; set; }
}

public class CardScanResultDto
{
    public string? RawText { get; set; }
    public string? Name { get; set; }
    public string? Email { get; set; }
    public string? Phone { get; set; }
    public string? Company { get; set; }
    public string? JobTitle { get; set; }
    public Dictionary<string, decimal>? ConfidenceScores { get; set; }
}

public class AudioTranscriptDto
{
    public string Text { get; set; } = string.Empty;
    public List<TranscriptSegmentDto>? Segments { get; set; }
}

public class TranscriptSegmentDto
{
    public string Text { get; set; } = string.Empty;
    public TimeSpan StartTime { get; set; }
    public TimeSpan EndTime { get; set; }
    public decimal Confidence { get; set; }
}

public class ExtractedTaskDto
{
    public string Description { get; set; } = string.Empty;
    public DateTime? DueDate { get; set; }
    public string? Priority { get; set; }
}


