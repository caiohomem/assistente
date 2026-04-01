using AssistenteExecutivo.Application.DTOs;
using MediatR;

namespace AssistenteExecutivo.Application.Commands.RelationshipTypes;

public class UpdateRelationshipTypeCommand : IRequest<RelationshipTypeDto>
{
    public Guid OwnerUserId { get; set; }
    public Guid RelationshipTypeId { get; set; }
    public string Name { get; set; } = string.Empty;
}
