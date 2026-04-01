using AssistenteExecutivo.Application.Commands.RelationshipTypes;
using AssistenteExecutivo.Application.DTOs;
using AssistenteExecutivo.Application.Interfaces;
using AssistenteExecutivo.Domain.Exceptions;
using AssistenteExecutivo.Domain.Interfaces;
using MediatR;

namespace AssistenteExecutivo.Application.Handlers.RelationshipTypes;

public class UpdateRelationshipTypeCommandHandler : IRequestHandler<UpdateRelationshipTypeCommand, RelationshipTypeDto>
{
    private readonly IRelationshipTypeRepository _relationshipTypeRepository;
    private readonly IRelationshipRepository _relationshipRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IClock _clock;

    public UpdateRelationshipTypeCommandHandler(
        IRelationshipTypeRepository relationshipTypeRepository,
        IRelationshipRepository relationshipRepository,
        IUnitOfWork unitOfWork,
        IClock clock)
    {
        _relationshipTypeRepository = relationshipTypeRepository;
        _relationshipRepository = relationshipRepository;
        _unitOfWork = unitOfWork;
        _clock = clock;
    }

    public async Task<RelationshipTypeDto> Handle(UpdateRelationshipTypeCommand request, CancellationToken cancellationToken)
    {
        if (request.OwnerUserId == Guid.Empty)
            throw new DomainException("Domain:OwnerUserIdObrigatorio");

        if (request.RelationshipTypeId == Guid.Empty)
            throw new DomainException("Domain:RelationshipTypeIdObrigatorio");

        if (string.IsNullOrWhiteSpace(request.Name))
            throw new DomainException("Domain:RelationshipTypeNomeObrigatorio");

        var trimmedName = request.Name.Trim();

        var relationshipType = await _relationshipTypeRepository.GetByIdAsync(
            request.RelationshipTypeId,
            request.OwnerUserId,
            cancellationToken);

        if (relationshipType == null)
            throw new DomainException("Domain:RelationshipTypeNaoEncontrado");

        var exists = await _relationshipTypeRepository.ExistsWithNameAsync(
            request.OwnerUserId,
            trimmedName,
            request.RelationshipTypeId,
            cancellationToken);

        if (exists)
            throw new DomainException("Domain:RelationshipTypeJaExiste");

        var nameChanged = !string.Equals(relationshipType.Name, trimmedName, StringComparison.Ordinal);

        relationshipType.UpdateName(trimmedName, _clock);
        relationshipType.MarkAsCustom(_clock);

        await _relationshipTypeRepository.UpdateAsync(relationshipType, cancellationToken);

        if (nameChanged)
        {
            await _relationshipRepository.UpdateRelationshipTypeNameAsync(
                relationshipType.RelationshipTypeId,
                relationshipType.Name,
                cancellationToken);
        }

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return new RelationshipTypeDto
        {
            RelationshipTypeId = relationshipType.RelationshipTypeId,
            Name = relationshipType.Name,
            IsDefault = relationshipType.IsDefault,
            CreatedAt = relationshipType.CreatedAt,
            UpdatedAt = relationshipType.UpdatedAt
        };
    }
}
