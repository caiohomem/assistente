using MediatR;

namespace AssistenteExecutivo.Application.Commands.Notes;

public class DeleteNoteCommand : IRequest
{
    public Guid NoteId { get; set; }
    public Guid OwnerUserId { get; set; }
}













