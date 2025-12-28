using AssistenteExecutivo.Domain.Notifications;
using MediatR;

namespace AssistenteExecutivo.Application.Commands.Notifications;

public class CreateEmailTemplateCommand : IRequest<Guid>
{
    public string Name { get; set; } = string.Empty;
    public EmailTemplateType TemplateType { get; set; }
    public string Subject { get; set; } = string.Empty;
    public string HtmlBody { get; set; } = string.Empty;
}

