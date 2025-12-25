using AssistenteExecutivo.Application.Commands.Contacts;
using AssistenteExecutivo.Application.Interfaces;
using AssistenteExecutivo.Domain.Exceptions;
using AssistenteExecutivo.Domain.ValueObjects;
using MediatR;

namespace AssistenteExecutivo.Application.Handlers.Contacts;

public class AddContactPhoneCommandHandler : IRequestHandler<AddContactPhoneCommand, Unit>
{
    private readonly IContactRepository _contactRepository;
    private readonly IUnitOfWork _unitOfWork;

    public AddContactPhoneCommandHandler(
        IContactRepository contactRepository,
        IUnitOfWork unitOfWork)
    {
        _contactRepository = contactRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<Unit> Handle(AddContactPhoneCommand request, CancellationToken cancellationToken)
    {
        if (request.OwnerUserId == Guid.Empty)
            throw new DomainException("Domain:OwnerUserIdObrigatorio");

        if (string.IsNullOrWhiteSpace(request.Phone))
            throw new DomainException("Domain:TelefoneObrigatorio");

        var contact = await _contactRepository.GetByIdAsync(request.ContactId, request.OwnerUserId, cancellationToken);
        if (contact == null)
            throw new DomainException("Domain:ContactNaoEncontrado");

        var phone = PhoneNumber.Create(request.Phone);
        contact.AddPhone(phone);

        await _contactRepository.UpdateAsync(contact, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Unit.Value;
    }
}

