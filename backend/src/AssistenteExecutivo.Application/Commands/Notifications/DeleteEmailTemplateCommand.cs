using MediatR;

namespace AssistenteExecutivo.Application.Commands.Notifications;

public class DeleteEmailTemplateCommand : IRequest
{
    public Guid EmailTemplateId { get; set; }
}

