using MediatR;

namespace AssistenteExecutivo.Application.Commands.Workflow;

/// <summary>
/// Archives (soft deletes) a workflow.
/// </summary>
public class ArchiveWorkflowCommand : IRequest<bool>
{
    public Guid WorkflowId { get; set; }
    public Guid OwnerUserId { get; set; }
}
