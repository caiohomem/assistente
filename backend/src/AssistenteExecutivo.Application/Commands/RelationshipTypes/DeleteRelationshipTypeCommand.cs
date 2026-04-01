using MediatR;

namespace AssistenteExecutivo.Application.Commands.RelationshipTypes;

public class DeleteRelationshipTypeCommand : IRequest<Unit>
{
    public Guid OwnerUserId { get; set; }
    public Guid RelationshipTypeId { get; set; }
}
