using AssistenteExecutivo.Application.DTOs;
using AssistenteExecutivo.Application.Interfaces;
using AssistenteExecutivo.Application.Queries.Contacts;
using MediatR;

namespace AssistenteExecutivo.Application.Handlers.Contacts;

public class GetContactByIdQueryHandler : IRequestHandler<GetContactByIdQuery, ContactDto?>
{
    private readonly IContactRepository _contactRepository;

    public GetContactByIdQueryHandler(IContactRepository contactRepository)
    {
        _contactRepository = contactRepository;
    }

    public async Task<ContactDto?> Handle(GetContactByIdQuery request, CancellationToken cancellationToken)
    {
        var contact = await _contactRepository.GetByIdAsync(
            request.ContactId,
            request.OwnerUserId,
            cancellationToken);

        if (contact == null)
            return null;

        return ContactDtoMapper.MapToDto(contact);
    }
}

