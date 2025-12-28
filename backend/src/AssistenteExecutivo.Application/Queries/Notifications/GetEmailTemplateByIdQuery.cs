using MediatR;

namespace AssistenteExecutivo.Application.Queries.Notifications;

public class GetEmailTemplateByIdQuery : IRequest<EmailTemplateDto?>
{
    public Guid EmailTemplateId { get; set; }
}

