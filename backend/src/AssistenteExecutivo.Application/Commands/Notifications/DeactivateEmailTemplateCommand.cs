using MediatR;

namespace AssistenteExecutivo.Application.Commands.Notifications;

public class DeactivateEmailTemplateCommand : IRequest
{
    public Guid EmailTemplateId { get; set; }
}

