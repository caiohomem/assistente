using AssistenteExecutivo.Domain.Enums;

namespace AssistenteExecutivo.Application.DTOs;

public class NoteDto
{
    public Guid NoteId { get; set; }
    public Guid ContactId { get; set; }
    public Guid AuthorId { get; set; }
    public NoteType Type { get; set; }
    public string RawContent { get; set; } = string.Empty;
    public string? StructuredData { get; set; }
    public int Version { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}










