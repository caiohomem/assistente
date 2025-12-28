using AssistenteExecutivo.Application.DTOs;
using AssistenteExecutivo.Application.Interfaces;
using AssistenteExecutivo.Application.Queries.Contacts;
using MediatR;

namespace AssistenteExecutivo.Application.Handlers.Contacts;

public class SearchContactsQueryHandler : IRequestHandler<SearchContactsQuery, SearchContactsResultDto>
{
    private readonly IContactRepository _contactRepository;

    public SearchContactsQueryHandler(IContactRepository contactRepository)
    {
        _contactRepository = contactRepository;
    }

    public async Task<SearchContactsResultDto> Handle(SearchContactsQuery request, CancellationToken cancellationToken)
    {
        // Validação de paginação
        var page = Math.Max(1, request.Page);
        var pageSize = Math.Max(1, Math.Min(100, request.PageSize)); // Máximo 100 itens por página

        // Buscar todos os contatos do usuário
        var allContacts = await _contactRepository.GetAllAsync(
            request.OwnerUserId,
            includeDeleted: false,
            cancellationToken);

        // Aplicar filtro de busca se fornecido
        var filteredContacts = allContacts.AsEnumerable();

        if (!string.IsNullOrWhiteSpace(request.SearchTerm))
        {
            var searchTerm = request.SearchTerm.Trim().ToLowerInvariant();

            filteredContacts = allContacts.Where(c =>
                // Buscar por nome
                (c.Name.FirstName.ToLowerInvariant().Contains(searchTerm) ||
                 (c.Name.LastName != null && c.Name.LastName.ToLowerInvariant().Contains(searchTerm)) ||
                 c.Name.FullName.ToLowerInvariant().Contains(searchTerm)) ||
                // Buscar por email
                c.Emails.Any(e => e.Value.Contains(searchTerm)) ||
                // Buscar por telefone
                c.Phones.Any(p => p.Number.Contains(searchTerm) || p.FormattedNumber.Contains(searchTerm)) ||
                // Buscar por empresa
                (!string.IsNullOrWhiteSpace(c.Company) && c.Company.ToLowerInvariant().Contains(searchTerm)) ||
                // Buscar por cargo
                (!string.IsNullOrWhiteSpace(c.JobTitle) && c.JobTitle.ToLowerInvariant().Contains(searchTerm)) ||
                // Buscar por tags
                c.Tags.Any(t => t.Value.Contains(searchTerm))
            );
        }

        var contactsList = filteredContacts.ToList();

        // Calcular paginação
        var total = contactsList.Count;
        var totalPages = (int)Math.Ceiling(total / (double)pageSize);
        var skip = (page - 1) * pageSize;

        // Aplicar paginação e ordenação
        var paginatedContacts = contactsList
            .OrderByDescending(c => c.CreatedAt)
            .Skip(skip)
            .Take(pageSize)
            .ToList();

        return new SearchContactsResultDto
        {
            Contacts = paginatedContacts.Select(ContactDtoMapper.MapToDto).ToList(),
            Total = total,
            Page = page,
            PageSize = pageSize,
            TotalPages = totalPages
        };
    }
}

