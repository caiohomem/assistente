using MediatR;

namespace AssistenteExecutivo.Application.Commands.Notifications;

public class ActivateEmailTemplateCommand : IRequest
{
    public Guid EmailTemplateId { get; set; }
}

