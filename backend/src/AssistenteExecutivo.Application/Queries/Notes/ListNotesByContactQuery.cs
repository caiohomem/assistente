using AssistenteExecutivo.Application.DTOs;
using MediatR;

namespace AssistenteExecutivo.Application.Queries.Notes;

public class ListNotesByContactQuery : IRequest<List<NoteDto>>
{
    public Guid ContactId { get; set; }
    public Guid OwnerUserId { get; set; }
}






