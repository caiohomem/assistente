using MediatR;

namespace AssistenteExecutivo.Application.Commands.Workflow;

/// <summary>
/// Activates a workflow for execution.
/// </summary>
public class ActivateWorkflowCommand : IRequest<bool>
{
    public Guid WorkflowId { get; set; }
    public Guid OwnerUserId { get; set; }
}
