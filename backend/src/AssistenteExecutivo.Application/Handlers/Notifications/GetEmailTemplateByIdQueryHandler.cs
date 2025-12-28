using AssistenteExecutivo.Application.Interfaces;
using AssistenteExecutivo.Application.Queries.Notifications;
using MediatR;

namespace AssistenteExecutivo.Application.Handlers.Notifications;

public class GetEmailTemplateByIdQueryHandler : IRequestHandler<GetEmailTemplateByIdQuery, EmailTemplateDto?>
{
    private readonly IEmailTemplateRepository _emailTemplateRepository;

    public GetEmailTemplateByIdQueryHandler(IEmailTemplateRepository emailTemplateRepository)
    {
        _emailTemplateRepository = emailTemplateRepository;
    }

    public async Task<EmailTemplateDto?> Handle(GetEmailTemplateByIdQuery request, CancellationToken cancellationToken)
    {
        var template = await _emailTemplateRepository.GetByIdAsync(request.EmailTemplateId, cancellationToken);
        if (template == null)
            return null;

        return new EmailTemplateDto
        {
            Id = template.Id,
            Name = template.Name,
            TemplateType = template.TemplateType,
            Subject = template.Subject,
            HtmlBody = template.HtmlBody,
            IsActive = template.IsActive,
            CreatedAt = template.CreatedAt,
            UpdatedAt = template.UpdatedAt,
            Placeholders = template.GetPlaceholders()
        };
    }
}

