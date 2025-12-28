using MediatR;

namespace AssistenteExecutivo.Application.Queries.Notifications;

public class ListEmailTemplatesQuery : IRequest<ListEmailTemplatesResultDto>
{
    public AssistenteExecutivo.Domain.Notifications.EmailTemplateType? TemplateType { get; set; }
    public bool? ActiveOnly { get; set; } = false;
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 20;
}

public class ListEmailTemplatesResultDto
{
    public List<EmailTemplateDto> Templates { get; set; } = new();
    public int Total { get; set; }
    public int Page { get; set; }
    public int PageSize { get; set; }
    public int TotalPages { get; set; }
}

public class EmailTemplateDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public AssistenteExecutivo.Domain.Notifications.EmailTemplateType TemplateType { get; set; }
    public string Subject { get; set; } = string.Empty;
    public string HtmlBody { get; set; } = string.Empty;
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public List<string> Placeholders { get; set; } = new();
}

