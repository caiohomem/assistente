namespace AssistenteExecutivo.Domain.Interfaces;

/// <summary>
/// Validation result for WorkflowSpec JSON.
/// </summary>
public class WorkflowSpecValidationResult
{
    public bool IsValid { get; set; }
    public List<string> Errors { get; set; } = new();
    public List<string> Warnings { get; set; } = new();

    public static WorkflowSpecValidationResult Success() => new() { IsValid = true };

    public static WorkflowSpecValidationResult Failure(params string[] errors)
        => new() { IsValid = false, Errors = errors.ToList() };

    public static WorkflowSpecValidationResult Failure(IEnumerable<string> errors)
        => new() { IsValid = false, Errors = errors.ToList() };
}

/// <summary>
/// Interface for validating WorkflowSpec JSON before compilation.
/// Validates schema, action types, endpoint allowlists, and security rules.
/// </summary>
public interface IWorkflowSpecValidator
{
    /// <summary>
    /// Validates a WorkflowSpec JSON string.
    /// </summary>
    /// <param name="specJson">The WorkflowSpec JSON to validate.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Validation result with errors and warnings.</returns>
    Task<WorkflowSpecValidationResult> ValidateAsync(string specJson, CancellationToken cancellationToken = default);
}
