using AssistenteExecutivo.Domain.Enums;
using MediatR;

namespace AssistenteExecutivo.Application.Queries.Automation;

public class ListTemplatesQuery : IRequest<ListTemplatesResultDto>
{
    public Guid OwnerUserId { get; set; }
    public TemplateType? Type { get; set; }
    public bool? ActiveOnly { get; set; } = false;
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 20;
}

public class ListTemplatesResultDto
{
    public List<TemplateDto> Templates { get; set; } = new();
    public int Total { get; set; }
    public int Page { get; set; }
    public int PageSize { get; set; }
    public int TotalPages { get; set; }
}

public class TemplateDto
{
    public Guid TemplateId { get; set; }
    public Guid OwnerUserId { get; set; }
    public string Name { get; set; } = string.Empty;
    public TemplateType Type { get; set; }
    public string Body { get; set; } = string.Empty;
    public string? PlaceholdersSchema { get; set; }
    public bool Active { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}





