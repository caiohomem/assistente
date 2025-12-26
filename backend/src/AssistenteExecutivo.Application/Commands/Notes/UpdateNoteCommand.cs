using MediatR;

namespace AssistenteExecutivo.Application.Commands.Notes;

public class UpdateNoteCommand : IRequest
{
    public Guid NoteId { get; set; }
    public Guid OwnerUserId { get; set; }
    public string? RawContent { get; set; }
    public string? StructuredData { get; set; }
}



