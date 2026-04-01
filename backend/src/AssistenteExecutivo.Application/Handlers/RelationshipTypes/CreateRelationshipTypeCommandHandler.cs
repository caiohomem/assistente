using AssistenteExecutivo.Application.Commands.RelationshipTypes;
using AssistenteExecutivo.Application.DTOs;
using AssistenteExecutivo.Application.Interfaces;
using AssistenteExecutivo.Domain.Entities;
using AssistenteExecutivo.Domain.Exceptions;
using AssistenteExecutivo.Domain.Interfaces;
using MediatR;

namespace AssistenteExecutivo.Application.Handlers.RelationshipTypes;

public class CreateRelationshipTypeCommandHandler : IRequestHandler<CreateRelationshipTypeCommand, RelationshipTypeDto>
{
    private readonly IRelationshipTypeRepository _relationshipTypeRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IClock _clock;

    public CreateRelationshipTypeCommandHandler(
        IRelationshipTypeRepository relationshipTypeRepository,
        IUnitOfWork unitOfWork,
        IClock clock)
    {
        _relationshipTypeRepository = relationshipTypeRepository;
        _unitOfWork = unitOfWork;
        _clock = clock;
    }

    public async Task<RelationshipTypeDto> Handle(CreateRelationshipTypeCommand request, CancellationToken cancellationToken)
    {
        if (request.OwnerUserId == Guid.Empty)
            throw new DomainException("Domain:OwnerUserIdObrigatorio");

        if (string.IsNullOrWhiteSpace(request.Name))
            throw new DomainException("Domain:RelationshipTypeNomeObrigatorio");

        var trimmedName = request.Name.Trim();

        var exists = await _relationshipTypeRepository.ExistsWithNameAsync(
            request.OwnerUserId,
            trimmedName,
            null,
            cancellationToken);

        if (exists)
            throw new DomainException("Domain:RelationshipTypeJaExiste");

        var relationshipType = new RelationshipType(
            Guid.NewGuid(),
            request.OwnerUserId,
            trimmedName,
            _clock);

        await _relationshipTypeRepository.AddAsync(relationshipType, cancellationToken);
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
