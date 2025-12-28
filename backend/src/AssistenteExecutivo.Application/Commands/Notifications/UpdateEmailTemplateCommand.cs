using MediatR;

namespace AssistenteExecutivo.Application.Commands.Notifications;

public class UpdateEmailTemplateCommand : IRequest
{
    public Guid EmailTemplateId { get; set; }
    public string? Name { get; set; }
    public string? Subject { get; set; }
    public string? HtmlBody { get; set; }
}

