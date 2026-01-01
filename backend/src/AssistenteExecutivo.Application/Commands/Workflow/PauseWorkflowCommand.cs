using MediatR;

namespace AssistenteExecutivo.Application.Commands.Workflow;

/// <summary>
/// Pauses an active workflow.
/// </summary>
public class PauseWorkflowCommand : IRequest<bool>
{
    public Guid WorkflowId { get; set; }
    public Guid OwnerUserId { get; set; }
}
