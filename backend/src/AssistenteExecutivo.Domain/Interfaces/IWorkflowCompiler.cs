namespace AssistenteExecutivo.Domain.Interfaces;

/// <summary>
/// Result of compiling a WorkflowSpec to n8n format.
/// </summary>
public class WorkflowCompilationResult
{
    public bool Success { get; set; }
    public string? CompiledJson { get; set; }
    public List<string> Errors { get; set; } = new();

    public static WorkflowCompilationResult Succeeded(string compiledJson)
        => new() { Success = true, CompiledJson = compiledJson };

    public static WorkflowCompilationResult Failed(params string[] errors)
        => new() { Success = false, Errors = errors.ToList() };

    public static WorkflowCompilationResult Failed(IEnumerable<string> errors)
        => new() { Success = false, Errors = errors.ToList() };
}

/// <summary>
/// Interface for compiling WorkflowSpec JSON into n8n workflow JSON.
/// Transforms the domain-specific spec format into n8n's workflow definition.
/// </summary>
public interface IWorkflowCompiler
{
    /// <summary>
    /// Compiles a validated WorkflowSpec JSON into n8n workflow format.
    /// </summary>
    /// <param name="name">Name of the workflow.</param>
    /// <param name="specJson">The validated WorkflowSpec JSON.</param>
    /// <param name="ownerUserId">Owner user ID for tagging.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Compilation result with n8n workflow JSON or errors.</returns>
    Task<WorkflowCompilationResult> CompileAsync(
        string name,
        string specJson,
        Guid ownerUserId,
        CancellationToken cancellationToken = default);
}
