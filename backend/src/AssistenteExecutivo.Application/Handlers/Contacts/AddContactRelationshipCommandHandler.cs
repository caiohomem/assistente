using AssistenteExecutivo.Application.Commands.Contacts;
using AssistenteExecutivo.Application.Interfaces;
using AssistenteExecutivo.Domain.Exceptions;
using AssistenteExecutivo.Domain.Interfaces;
using MediatR;

namespace AssistenteExecutivo.Application.Handlers.Contacts;

public class AddContactRelationshipCommandHandler : IRequestHandler<AddContactRelationshipCommand, Unit>
{
    private readonly IContactRepository _contactRepository;
    private readonly IRelationshipRepository _relationshipRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IClock _clock;

    public AddContactRelationshipCommandHandler(
        IContactRepository contactRepository,
        IRelationshipRepository relationshipRepository,
        IUnitOfWork unitOfWork,
        IClock clock)
    {
        _contactRepository = contactRepository;
        _relationshipRepository = relationshipRepository;
        _unitOfWork = unitOfWork;
        _clock = clock;
    }

    public async Task<Unit> Handle(AddContactRelationshipCommand request, CancellationToken cancellationToken)
    {
        if (request.OwnerUserId == Guid.Empty)
            throw new DomainException("Domain:OwnerUserIdObrigatorio");

        if (string.IsNullOrWhiteSpace(request.Type))
            throw new DomainException("Domain:RelationshipTypeObrigatorio");

        var contact = await _contactRepository.GetByIdAsync(request.ContactId, request.OwnerUserId, cancellationToken);
        if (contact == null)
            throw new DomainException("Domain:ContactNaoEncontrado");

        // Verify target contact exists and belongs to the same owner (sem carregar a entidade completa)
        var targetContactExists = await _contactRepository.ExistsAsync(request.TargetContactId, request.OwnerUserId, cancellationToken);
        if (!targetContactExists)
        {
            // Diagnostic check: verify if contact exists but with different owner or is deleted
            var (exists, ownerUserId, isDeleted) = await _contactRepository.GetContactStatusAsync(request.TargetContactId, cancellationToken);
            
            if (!exists)
                throw new DomainException("Domain:TargetContactNaoEncontrado");
            
            if (isDeleted)
                throw new DomainException("Domain:TargetContactDeletado");
            
            if (ownerUserId.HasValue && ownerUserId.Value != request.OwnerUserId)
                throw new DomainException("Domain:TargetContactPertenceOutroUsuario");
            
            // Fallback if we can't determine the exact reason
            throw new DomainException("Domain:TargetContactNaoEncontrado");
        }

        var relationshipId = Guid.NewGuid();
        var relationship = contact.AddRelationship(
            relationshipId,
            request.TargetContactId,
            request.Type,
            request.Description,
            _clock);

        // Garanta que o EF trate o relacionamento como nova entidade (INSERT), mesmo com PK preenchida.
        await _relationshipRepository.AddAsync(relationship, cancellationToken);

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Unit.Value;
    }
}
