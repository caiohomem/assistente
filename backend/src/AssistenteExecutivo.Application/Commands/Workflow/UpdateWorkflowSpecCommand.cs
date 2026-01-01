using MediatR;

namespace AssistenteExecutivo.Application.Commands.Workflow;

/// <summary>
/// Updates the spec of an existing workflow.
/// Increments version, recompiles to n8n, and updates in n8n.
/// </summary>
public class UpdateWorkflowSpecCommand : IRequest<UpdateWorkflowResult>
{
    public Guid WorkflowId { get; set; }
    public Guid OwnerUserId { get; set; }
    public string SpecJson { get; set; } = string.Empty;
}

public class UpdateWorkflowResult
{
    public bool Success { get; set; }
    public int? NewVersion { get; set; }
    public List<string> Errors { get; set; } = new();

    public static UpdateWorkflowResult Succeeded(int newVersion)
        => new() { Success = true, NewVersion = newVersion };

    public static UpdateWorkflowResult Failed(params string[] errors)
        => new() { Success = false, Errors = errors.ToList() };
}
