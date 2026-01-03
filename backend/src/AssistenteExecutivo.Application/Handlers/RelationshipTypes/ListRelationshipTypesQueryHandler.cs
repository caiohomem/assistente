using AssistenteExecutivo.Application.DTOs;
using AssistenteExecutivo.Application.Interfaces;
using AssistenteExecutivo.Application.Queries.RelationshipTypes;
using AssistenteExecutivo.Domain.Constants;
using AssistenteExecutivo.Domain.Entities;
using AssistenteExecutivo.Domain.Exceptions;
using AssistenteExecutivo.Domain.Interfaces;
using MediatR;

namespace AssistenteExecutivo.Application.Handlers.RelationshipTypes;

public class ListRelationshipTypesQueryHandler : IRequestHandler<ListRelationshipTypesQuery, List<RelationshipTypeDto>>
{
    private readonly IRelationshipTypeRepository _relationshipTypeRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IClock _clock;

    public ListRelationshipTypesQueryHandler(
        IRelationshipTypeRepository relationshipTypeRepository,
        IUnitOfWork unitOfWork,
        IClock clock)
    {
        _relationshipTypeRepository = relationshipTypeRepository;
        _unitOfWork = unitOfWork;
        _clock = clock;
    }

    public async Task<List<RelationshipTypeDto>> Handle(ListRelationshipTypesQuery request, CancellationToken cancellationToken)
    {
        if (request.OwnerUserId == Guid.Empty)
            throw new DomainException("Domain:OwnerUserIdObrigatorio");

        var types = await _relationshipTypeRepository.GetByOwnerAsync(request.OwnerUserId, cancellationToken);

        if (types.Count == 0)
        {
            var defaults = RelationshipTypeDefaults.Names
                .Select(name => new RelationshipType(Guid.NewGuid(), request.OwnerUserId, name, _clock, isDefault: true))
                .ToList();

            await _relationshipTypeRepository.AddRangeAsync(defaults, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            types = defaults;
        }

        return types
            .OrderBy(t => t.CreatedAt)
            .Select(t => new RelationshipTypeDto
            {
                RelationshipTypeId = t.RelationshipTypeId,
                Name = t.Name,
                IsDefault = t.IsDefault,
                CreatedAt = t.CreatedAt,
                UpdatedAt = t.UpdatedAt
            })
            .ToList();
    }
}
