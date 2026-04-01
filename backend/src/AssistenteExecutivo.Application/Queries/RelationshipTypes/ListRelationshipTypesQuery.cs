using AssistenteExecutivo.Application.DTOs;
using MediatR;

namespace AssistenteExecutivo.Application.Queries.RelationshipTypes;

public class ListRelationshipTypesQuery : IRequest<List<RelationshipTypeDto>>
{
    public Guid OwnerUserId { get; set; }
}
