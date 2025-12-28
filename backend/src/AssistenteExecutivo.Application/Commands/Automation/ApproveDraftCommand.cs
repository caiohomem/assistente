using MediatR;

namespace AssistenteExecutivo.Application.Commands.Automation;

public class ApproveDraftCommand : IRequest
{
    public Guid DraftId { get; set; }
    public Guid OwnerUserId { get; set; }
}





