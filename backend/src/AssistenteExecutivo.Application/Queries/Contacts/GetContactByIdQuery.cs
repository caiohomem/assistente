using AssistenteExecutivo.Application.DTOs;
using MediatR;

namespace AssistenteExecutivo.Application.Queries.Contacts;

public class GetContactByIdQuery : IRequest<ContactDto?>
{
    public Guid ContactId { get; set; }
    public Guid OwnerUserId { get; set; }
}


