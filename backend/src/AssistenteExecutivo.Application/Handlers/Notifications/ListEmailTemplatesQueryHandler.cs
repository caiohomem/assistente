using AssistenteExecutivo.Application.Interfaces;
using AssistenteExecutivo.Application.Queries.Notifications;
using MediatR;

namespace AssistenteExecutivo.Application.Handlers.Notifications;

public class ListEmailTemplatesQueryHandler : IRequestHandler<ListEmailTemplatesQuery, ListEmailTemplatesResultDto>
{
    private readonly IEmailTemplateRepository _emailTemplateRepository;

    public ListEmailTemplatesQueryHandler(IEmailTemplateRepository emailTemplateRepository)
    {
        _emailTemplateRepository = emailTemplateRepository;
    }

    public async Task<ListEmailTemplatesResultDto> Handle(ListEmailTemplatesQuery request, CancellationToken cancellationToken)
    {
        var page = Math.Max(1, request.Page);
        var pageSize = Math.Max(1, Math.Min(100, request.PageSize));

        var templates = await _emailTemplateRepository.GetByTypeFilterAsync(
            request.TemplateType,
            request.ActiveOnly,
            cancellationToken);

        var total = templates.Count;
        var totalPages = (int)Math.Ceiling(total / (double)pageSize);
        var skip = (page - 1) * pageSize;

        var paginatedTemplates = templates
            .OrderByDescending(t => t.CreatedAt)
            .Skip(skip)
            .Take(pageSize)
            .Select(t => new EmailTemplateDto
            {
                Id = t.Id,
                Name = t.Name,
                TemplateType = t.TemplateType,
                Subject = t.Subject,
                HtmlBody = t.HtmlBody,
                IsActive = t.IsActive,
                CreatedAt = t.CreatedAt,
                UpdatedAt = t.UpdatedAt,
                Placeholders = t.GetPlaceholders()
            })
            .ToList();

        return new ListEmailTemplatesResultDto
        {
            Templates = paginatedTemplates,
            Total = total,
            Page = page,
            PageSize = pageSize,
            TotalPages = totalPages
        };
    }
}

