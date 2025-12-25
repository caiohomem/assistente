using AssistenteExecutivo.Application.DTOs;
using AssistenteExecutivo.Application.Interfaces;
using AssistenteExecutivo.Application.Queries.Contacts;
using AssistenteExecutivo.Domain.Entities;
using MediatR;

namespace AssistenteExecutivo.Application.Handlers.Contacts;

public class ListContactsQueryHandler : IRequestHandler<ListContactsQuery, ListContactsResultDto>
{
    private readonly IContactRepository _contactRepository;

    public ListContactsQueryHandler(IContactRepository contactRepository)
    {
        _contactRepository = contactRepository;
    }

    public async Task<ListContactsResultDto> Handle(ListContactsQuery request, CancellationToken cancellationToken)
    {
        // Validação de paginação
        var page = Math.Max(1, request.Page);
        var pageSize = Math.Max(1, Math.Min(100, request.PageSize)); // Máximo 100 itens por página

        // Buscar todos os contatos
        var allContacts = await _contactRepository.GetAllAsync(
            request.OwnerUserId,
            request.IncludeDeleted,
            cancellationToken);

        // Calcular paginação
        var total = allContacts.Count;
        var totalPages = (int)Math.Ceiling(total / (double)pageSize);
        var skip = (page - 1) * pageSize;

        // Aplicar paginação
        var paginatedContacts = allContacts
            .OrderByDescending(c => c.CreatedAt)
            .Skip(skip)
            .Take(pageSize)
            .ToList();

        return new ListContactsResultDto
        {
            Contacts = paginatedContacts.Select(ContactDtoMapper.MapToDto).ToList(),
            Total = total,
            Page = page,
            PageSize = pageSize,
            TotalPages = totalPages
        };
    }
}

