using MediatR;

namespace AssistenteExecutivo.Application.Commands.Contacts;

public class DeleteRelationshipCommand : IRequest
{
    public Guid RelationshipId { get; set; }
    public Guid OwnerUserId { get; set; }
}


