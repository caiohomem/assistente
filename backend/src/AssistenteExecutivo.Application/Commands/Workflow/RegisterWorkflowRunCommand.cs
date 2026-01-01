using MediatR;

namespace AssistenteExecutivo.Application.Commands.Workflow;

public class RegisterWorkflowRunCommand : IRequest<RegisterWorkflowRunResult>
{
    public string RunId { get; set; } = string.Empty;
    public string WorkflowId { get; set; } = string.Empty;
    public string TenantId { get; set; } = string.Empty;
    public string RequestedBy { get; set; } = string.Empty;
    public string? IdempotencyKey { get; set; }
    public object? Inputs { get; set; }
    public string? StartedAt { get; set; }
    public string Status { get; set; } = "Running";
}

public class RegisterWorkflowRunResult
{
    public bool Success { get; set; }
    public string? RunId { get; set; }
    public string? ErrorMessage { get; set; }

    public static RegisterWorkflowRunResult Succeeded(string runId)
        => new() { Success = true, RunId = runId };

    public static RegisterWorkflowRunResult Failed(string error)
        => new() { Success = false, ErrorMessage = error };
}
