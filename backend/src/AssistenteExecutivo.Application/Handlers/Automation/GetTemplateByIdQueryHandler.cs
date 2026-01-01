using AssistenteExecutivo.Application.Interfaces;
using AssistenteExecutivo.Application.Queries.Automation;
using MediatR;

namespace AssistenteExecutivo.Application.Handlers.Automation;

public class GetTemplateByIdQueryHandler : IRequestHandler<GetTemplateByIdQuery, TemplateDto?>
{
    private readonly ITemplateRepository _templateRepository;

    public GetTemplateByIdQueryHandler(ITemplateRepository templateRepository)
    {
        _templateRepository = templateRepository;
    }

    public async Task<TemplateDto?> Handle(GetTemplateByIdQuery request, CancellationToken cancellationToken)
    {
        var template = await _templateRepository.GetByIdAsync(request.TemplateId, request.OwnerUserId, cancellationToken);
        if (template == null)
            return null;

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







