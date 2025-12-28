using AssistenteExecutivo.Application.Interfaces;
using AssistenteExecutivo.Application.Queries.Automation;
using MediatR;

namespace AssistenteExecutivo.Application.Handlers.Automation;

public class ListTemplatesQueryHandler : IRequestHandler<ListTemplatesQuery, ListTemplatesResultDto>
{
    private readonly ITemplateRepository _templateRepository;

    public ListTemplatesQueryHandler(ITemplateRepository templateRepository)
    {
        _templateRepository = templateRepository;
    }

    public async Task<ListTemplatesResultDto> Handle(ListTemplatesQuery request, CancellationToken cancellationToken)
    {
        var page = Math.Max(1, request.Page);
        var pageSize = Math.Max(1, Math.Min(100, request.PageSize));

        List<Domain.Entities.Template> templates;

        if (request.ActiveOnly == true)
        {
            if (request.Type.HasValue)
            {
                templates = await _templateRepository.GetActiveByTypeAsync(request.Type.Value, request.OwnerUserId, cancellationToken);
            }
            else
            {
                templates = await _templateRepository.GetActiveByOwnerIdAsync(request.OwnerUserId, cancellationToken);
            }
        }
        else if (request.Type.HasValue)
        {
            templates = await _templateRepository.GetByTypeAsync(request.Type.Value, request.OwnerUserId, cancellationToken);
        }
        else
        {
            templates = await _templateRepository.GetByOwnerIdAsync(request.OwnerUserId, cancellationToken);
        }

        var total = templates.Count;
        var totalPages = (int)Math.Ceiling(total / (double)pageSize);
        var skip = (page - 1) * pageSize;

        var paginatedTemplates = templates
            .OrderByDescending(t => t.CreatedAt)
            .Skip(skip)
            .Take(pageSize)
            .ToList();

        return new ListTemplatesResultDto
        {
            Templates = paginatedTemplates.Select(MapToDto).ToList(),
            Total = total,
            Page = page,
            PageSize = pageSize,
            TotalPages = totalPages
        };
    }

    private static TemplateDto MapToDto(Domain.Entities.Template template)
    {
        return new TemplateDto
        {
            TemplateId = template.TemplateId,
            OwnerUserId = template.OwnerUserId,
            Name = template.Name,
            Type = template.Type,
            Body = template.Body,
            PlaceholdersSchema = template.PlaceholdersSchema,
            Active = template.Active,
            CreatedAt = template.CreatedAt,
            UpdatedAt = template.UpdatedAt
        };
    }
}





