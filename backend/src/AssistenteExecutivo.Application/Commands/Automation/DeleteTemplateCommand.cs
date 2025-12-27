using MediatR;

namespace AssistenteExecutivo.Application.Commands.Automation;

public class DeleteTemplateCommand : IRequest
{
    public Guid TemplateId { get; set; }
    public Guid OwnerUserId { get; set; }
}

