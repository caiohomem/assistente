using AssistenteExecutivo.Application.Commands.Contacts;
using AssistenteExecutivo.Application.Interfaces;
using AssistenteExecutivo.Domain.Exceptions;
using MediatR;

namespace AssistenteExecutivo.Application.Handlers.Contacts;

public class DeleteContactCommandHandler : IRequestHandler<DeleteContactCommand, Unit>
{
    private readonly IContactRepository _contactRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IPublisher _publisher;

    public DeleteContactCommandHandler(
        IContactRepository contactRepository,
        IUnitOfWork unitOfWork,
        IPublisher publisher)
    {
        _contactRepository = contactRepository;
        _unitOfWork = unitOfWork;
        _publisher = publisher;
    }

    public async Task<Unit> Handle(DeleteContactCommand request, CancellationToken cancellationToken)
    {
        if (request.OwnerUserId == Guid.Empty)
            throw new DomainException("Domain:OwnerUserIdObrigatorio");

        var contact = await _contactRepository.GetByIdAsync(request.ContactId, request.OwnerUserId, cancellationToken);
        if (contact == null)
            throw new DomainException("Domain:ContactNaoEncontrado");

        await _contactRepository.DeleteAsync(contact, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        // Publish domain events
        foreach (var domainEvent in contact.DomainEvents)
        {
            await _publisher.Publish(domainEvent, cancellationToken);
        }
        contact.ClearDomainEvents();

        return Unit.Value;
    }
}

