using AssistenteExecutivo.Application.DTOs;
using MediatR;

namespace AssistenteExecutivo.Application.Queries.Notes;

public class GetNoteByIdQuery : IRequest<NoteDto?>
{
    public Guid NoteId { get; set; }
    public Guid OwnerUserId { get; set; }
}














