using AssistenteExecutivo.Application.DTOs;
using MediatR;

namespace AssistenteExecutivo.Application.Commands.Capture;

public class ProcessAudioNoteCommand : IRequest<ProcessAudioNoteCommandResult>
{
    public Guid OwnerUserId { get; set; }
    public Guid ContactId { get; set; }
    public byte[] AudioBytes { get; set; } = Array.Empty<byte>();
    public string FileName { get; set; } = string.Empty;
    public string MimeType { get; set; } = string.Empty;
}

public class ProcessAudioNoteCommandResult
{
    public Guid NoteId { get; set; }
    public Guid JobId { get; set; }
    public Guid MediaId { get; set; }
    
    // Dados completos do job para evitar polling
    public string Status { get; set; } = string.Empty;
    public AudioTranscriptDto? AudioTranscript { get; set; }
    public string? AudioSummary { get; set; }
    public List<ExtractedTaskDto>? ExtractedTasks { get; set; }
    public DateTime RequestedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
    public string? ErrorCode { get; set; }
    public string? ErrorMessage { get; set; }
    
    // Áudio de resposta gerado via TTS (opcional)
    public Guid? ResponseMediaId { get; set; }
}


