using AssistenteExecutivo.Application.Commands.Contacts;
using AssistenteExecutivo.Application.Interfaces;
using AssistenteExecutivo.Domain.Exceptions;
using MediatR;

namespace AssistenteExecutivo.Application.Handlers.Contacts;

public class DeleteRelationshipCommandHandler : IRequestHandler<DeleteRelationshipCommand>
{
    private readonly IRelationshipRepository _relationshipRepository;
    private readonly IContactRepository _contactRepository;
    private readonly IUnitOfWork _unitOfWork;

    public DeleteRelationshipCommandHandler(
        IRelationshipRepository relationshipRepository,
        IContactRepository contactRepository,
        IUnitOfWork unitOfWork)
    {
        _relationshipRepository = relationshipRepository;
        _contactRepository = contactRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task Handle(DeleteRelationshipCommand request, CancellationToken cancellationToken)
    {
        if (request.RelationshipId == Guid.Empty)
            throw new DomainException("Domain:RelationshipIdObrigatorio");

        if (request.OwnerUserId == Guid.Empty)
            throw new DomainException("Domain:OwnerUserIdObrigatorio");

        // Buscar o relacionamento
        var relationship = await _relationshipRepository.GetByIdAsync(request.RelationshipId, cancellationToken);
        if (relationship == null)
            throw new DomainException("Domain:RelationshipNaoEncontrado");

        // Verificar se o relacionamento pertence ao usuário através do contato fonte
        var (exists, ownerUserId, isDeleted) = await _contactRepository.GetContactStatusAsync(relationship.SourceContactId, cancellationToken);
        if (!exists || isDeleted)
            throw new DomainException("Domain:ContactNaoEncontrado");

        if (ownerUserId != request.OwnerUserId)
            throw new DomainException("Domain:RelationshipNaoPertenceAoUsuario");

        // Deletar o relacionamento
        await _relationshipRepository.DeleteAsync(relationship, cancellationToken);

        // Salvar alterações
        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }
}


