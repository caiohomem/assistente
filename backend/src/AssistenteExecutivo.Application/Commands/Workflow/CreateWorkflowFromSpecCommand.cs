using MediatR;

namespace AssistenteExecutivo.Application.Commands.Workflow;

/// <summary>
/// Creates a new workflow from a WorkflowSpec JSON.
/// Validates spec, compiles to n8n format, creates in n8n, and persists.
/// </summary>
public class CreateWorkflowFromSpecCommand : IRequest<CreateWorkflowResult>
{
    public Guid OwnerUserId { get; set; }
    public string SpecJson { get; set; } = string.Empty;
    public bool ActivateImmediately { get; set; }
}

public class CreateWorkflowResult
{
    public bool Success { get; set; }
    public Guid? WorkflowId { get; set; }
    public string? N8nWorkflowId { get; set; }
    public List<string> Errors { get; set; } = new();

    public static CreateWorkflowResult Succeeded(Guid workflowId, string n8nWorkflowId)
        => new() { Success = true, WorkflowId = workflowId, N8nWorkflowId = n8nWorkflowId };

    public static CreateWorkflowResult Failed(params string[] errors)
        => new() { Success = false, Errors = errors.ToList() };

    public static CreateWorkflowResult Failed(IEnumerable<string> errors)
        => new() { Success = false, Errors = errors.ToList() };
}
