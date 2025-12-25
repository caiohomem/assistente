using AssistenteExecutivo.Application.DTOs;
using MediatR;

namespace AssistenteExecutivo.Application.Queries.Contacts;

public class ListContactsQuery : IRequest<ListContactsResultDto>
{
    public Guid OwnerUserId { get; set; }
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 20;
    public bool IncludeDeleted { get; set; } = false;
}

public class ListContactsResultDto
{
    public List<ContactDto> Contacts { get; set; } = new();
    public int Total { get; set; }
    public int Page { get; set; }
    public int PageSize { get; set; }
    public int TotalPages { get; set; }
}


