using MediatR;

namespace AssistenteExecutivo.Application.Commands.Workflow;

/// <summary>
/// Approves a workflow step that is waiting for approval.
/// </summary>
public class ApproveWorkflowStepCommand : IRequest<ApproveStepResult>
{
    public Guid ExecutionId { get; set; }
    public Guid ApprovedByUserId { get; set; }
}

public class ApproveStepResult
{
    public bool Success { get; set; }
    public string? ErrorMessage { get; set; }

    public static ApproveStepResult Succeeded() => new() { Success = true };

    public static ApproveStepResult Failed(string errorMessage)
        => new() { Success = false, ErrorMessage = errorMessage };
}
