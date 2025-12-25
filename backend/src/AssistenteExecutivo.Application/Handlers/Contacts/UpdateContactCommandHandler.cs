using AssistenteExecutivo.Application.Commands.Contacts;
using AssistenteExecutivo.Application.Interfaces;
using AssistenteExecutivo.Domain.Exceptions;
using AssistenteExecutivo.Domain.ValueObjects;
using MediatR;

namespace AssistenteExecutivo.Application.Handlers.Contacts;

public class UpdateContactCommandHandler : IRequestHandler<UpdateContactCommand, Unit>
{
    private readonly IContactRepository _contactRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IPublisher _publisher;

    public UpdateContactCommandHandler(
        IContactRepository contactRepository,
        IUnitOfWork unitOfWork,
        IPublisher publisher)
    {
        _contactRepository = contactRepository;
        _unitOfWork = unitOfWork;
        _publisher = publisher;
    }

    public async Task<Unit> Handle(UpdateContactCommand request, CancellationToken cancellationToken)
    {
        if (request.OwnerUserId == Guid.Empty)
            throw new DomainException("Domain:OwnerUserIdObrigatorio");

        var contact = await _contactRepository.GetByIdAsync(request.ContactId, request.OwnerUserId, cancellationToken);
        if (contact == null)
            throw new DomainException("Domain:ContactNaoEncontrado");

        PersonName? name = null;
        if (!string.IsNullOrWhiteSpace(request.FirstName))
        {
            name = PersonName.Create(request.FirstName, request.LastName);
        }

        Address? address = null;
        if (!string.IsNullOrWhiteSpace(request.Street) ||
            !string.IsNullOrWhiteSpace(request.City) ||
            !string.IsNullOrWhiteSpace(request.State) ||
            !string.IsNullOrWhiteSpace(request.ZipCode) ||
            !string.IsNullOrWhiteSpace(request.Country))
        {
            address = Address.Create(
                request.Street,
                request.City,
                request.State,
                request.ZipCode,
                request.Country);
        }

        contact.UpdateDetails(
            name: name,
            jobTitle: request.JobTitle,
            company: request.Company,
            address: address);

        // Validação: contato deve ter pelo menos um email ou telefone
        contact.EnsureHasEmailOrPhone();

        await _contactRepository.UpdateAsync(contact, cancellationToken);
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

