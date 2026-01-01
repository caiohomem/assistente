using MediatR;

namespace AssistenteExecutivo.Application.Commands.Workflow;

/// <summary>
/// Executes a workflow via n8n.
/// </summary>
public class ExecuteWorkflowCommand : IRequest<ExecuteWorkflowResult>
{
    public Guid WorkflowId { get; set; }
    public Guid OwnerUserId { get; set; }
    public string? InputJson { get; set; }
}

public class ExecuteWorkflowResult
{
    public bool Success { get; set; }
    public Guid? ExecutionId { get; set; }
    public string? N8nExecutionId { get; set; }
    public string? ErrorMessage { get; set; }

    public static ExecuteWorkflowResult Succeeded(Guid executionId, string n8nExecutionId)
        => new() { Success = true, ExecutionId = executionId, N8nExecutionId = n8nExecutionId };

    public static ExecuteWorkflowResult Failed(string errorMessage)
        => new() { Success = false, ErrorMessage = errorMessage };
}
