using AssistenteExecutivo.Application.Commands.Contacts;
using AssistenteExecutivo.Application.Interfaces;
using AssistenteExecutivo.Domain.Entities;
using AssistenteExecutivo.Domain.Exceptions;
using AssistenteExecutivo.Domain.Interfaces;
using AssistenteExecutivo.Domain.ValueObjects;
using MediatR;

namespace AssistenteExecutivo.Application.Handlers.Contacts;

public class CreateContactCommandHandler : IRequestHandler<CreateContactCommand, Guid>
{
    private readonly IContactRepository _contactRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IClock _clock;
    private readonly IPublisher _publisher;

    public CreateContactCommandHandler(
        IContactRepository contactRepository,
        IUnitOfWork unitOfWork,
        IClock clock,
        IPublisher publisher)
    {
        _contactRepository = contactRepository;
        _unitOfWork = unitOfWork;
        _clock = clock;
        _publisher = publisher;
    }

    public async Task<Guid> Handle(CreateContactCommand request, CancellationToken cancellationToken)
    {
        if (request.OwnerUserId == Guid.Empty)
            throw new DomainException("Domain:OwnerUserIdObrigatorio");

        var contactId = Guid.NewGuid();
        var name = PersonName.Create(request.FirstName, request.LastName);

        var contact = Contact.Create(contactId, request.OwnerUserId, name, _clock);

        if (!string.IsNullOrWhiteSpace(request.JobTitle))
        {
            contact.UpdateDetails(jobTitle: request.JobTitle);
        }

        if (!string.IsNullOrWhiteSpace(request.Company))
        {
            contact.UpdateDetails(company: request.Company);
        }

        if (!string.IsNullOrWhiteSpace(request.Street) ||
            !string.IsNullOrWhiteSpace(request.City) ||
            !string.IsNullOrWhiteSpace(request.State) ||
            !string.IsNullOrWhiteSpace(request.ZipCode) ||
            !string.IsNullOrWhiteSpace(request.Country))
        {
            var address = Address.Create(
                request.Street,
                request.City,
                request.State,
                request.ZipCode,
                request.Country);
            contact.UpdateDetails(address: address);
        }

        await _contactRepository.AddAsync(contact, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        // Publish domain events
        foreach (var domainEvent in contact.DomainEvents)
        {
            await _publisher.Publish(domainEvent, cancellationToken);
        }
        contact.ClearDomainEvents();

        return contactId;
    }
}

