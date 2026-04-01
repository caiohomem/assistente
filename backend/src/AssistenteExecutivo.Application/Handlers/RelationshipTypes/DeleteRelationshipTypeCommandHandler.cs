using AssistenteExecutivo.Application.Commands.RelationshipTypes;
using AssistenteExecutivo.Application.Interfaces;
using AssistenteExecutivo.Domain.Exceptions;
using MediatR;

namespace AssistenteExecutivo.Application.Handlers.RelationshipTypes;

public class DeleteRelationshipTypeCommandHandler : IRequestHandler<DeleteRelationshipTypeCommand, Unit>
{
    private readonly IRelationshipTypeRepository _relationshipTypeRepository;
    private readonly IRelationshipRepository _relationshipRepository;
    private readonly IUnitOfWork _unitOfWork;

    public DeleteRelationshipTypeCommandHandler(
        IRelationshipTypeRepository relationshipTypeRepository,
        IRelationshipRepository relationshipRepository,
        IUnitOfWork unitOfWork)
    {
        _relationshipTypeRepository = relationshipTypeRepository;
        _relationshipRepository = relationshipRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<Unit> Handle(DeleteRelationshipTypeCommand request, CancellationToken cancellationToken)
    {
        if (request.OwnerUserId == Guid.Empty)
            throw new DomainException("Domain:OwnerUserIdObrigatorio");

        if (request.RelationshipTypeId == Guid.Empty)
            throw new DomainException("Domain:RelationshipTypeIdObrigatorio");

        var relationshipType = await _relationshipTypeRepository.GetByIdAsync(
            request.RelationshipTypeId,
            request.OwnerUserId,
            cancellationToken);

        if (relationshipType == null)
            throw new DomainException("Domain:RelationshipTypeNaoEncontrado");

        await _relationshipRepository.RemoveRelationshipTypeReferenceAsync(
            relationshipType.RelationshipTypeId,
            cancellationToken);

        await _relationshipTypeRepository.DeleteAsync(relationshipType, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Unit.Value;
    }
}
