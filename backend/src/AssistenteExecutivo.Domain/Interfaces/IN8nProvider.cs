namespace AssistenteExecutivo.Domain.Interfaces;

/// <summary>
/// Result of creating a workflow in n8n.
/// </summary>
public class N8nWorkflowResult
{
    public bool Success { get; set; }
    public string? N8nWorkflowId { get; set; }
    public string? ErrorMessage { get; set; }

    public static N8nWorkflowResult Succeeded(string n8nWorkflowId)
        => new() { Success = true, N8nWorkflowId = n8nWorkflowId };

    public static N8nWorkflowResult Failed(string errorMessage)
        => new() { Success = false, ErrorMessage = errorMessage };
}

/// <summary>
/// Result of executing a workflow in n8n.
/// </summary>
public class N8nExecutionResult
{
    public bool Success { get; set; }
    public string? N8nExecutionId { get; set; }
    public string? ErrorMessage { get; set; }

    public static N8nExecutionResult Succeeded(string n8nExecutionId)
        => new() { Success = true, N8nExecutionId = n8nExecutionId };

    public static N8nExecutionResult Failed(string errorMessage)
        => new() { Success = false, ErrorMessage = errorMessage };
}

/// <summary>
/// Status of an execution in n8n.
/// </summary>
public class N8nExecutionStatus
{
    public string N8nExecutionId { get; set; } = null!;
    public bool IsRunning { get; set; }
    public bool IsCompleted { get; set; }
    public bool IsFailed { get; set; }
    public bool IsWaitingForApproval { get; set; }
    public int? CurrentStepIndex { get; set; }
    public string? OutputJson { get; set; }
    public string? ErrorMessage { get; set; }
}

/// <summary>
/// Result from the Flow Builder system workflow.
/// </summary>
public class FlowBuilderResult
{
    public bool IsSuccess { get; init; }
    public string? WorkflowId { get; init; }
    public string? SpecId { get; init; }
    public int SpecVersion { get; init; }
    public List<string> Warnings { get; init; } = new();
    public string? ErrorMessage { get; init; }

    public static FlowBuilderResult Succeeded(string workflowId, string specId, int specVersion, List<string> warnings)
        => new()
        {
            IsSuccess = true,
            WorkflowId = workflowId,
            SpecId = specId,
            SpecVersion = specVersion,
            Warnings = warnings
        };

    public static FlowBuilderResult Failed(string error)
        => new() { IsSuccess = false, ErrorMessage = error };
}

/// <summary>
/// Result from the Flow Runner system workflow.
/// </summary>
public class FlowRunnerResult
{
    public bool IsSuccess { get; init; }
    public string? RunId { get; init; }
    public string? ExecutionId { get; init; }
    public string? Status { get; init; }
    public object? Result { get; init; }
    public string? Error { get; init; }
    public bool IsAsync { get; init; }
    public string? StartedAt { get; init; }
    public string? FinishedAt { get; init; }
    public string? ErrorMessage { get; init; }

    public static FlowRunnerResult Failed(string error)
        => new() { IsSuccess = false, ErrorMessage = error };
}

/// <summary>
/// Interface for interacting with n8n API.
/// Handles workflow lifecycle and execution management.
/// </summary>
public interface IN8nProvider
{
    /// <summary>
    /// Creates a new workflow in n8n.
    /// </summary>
    /// <param name="name">Workflow name.</param>
    /// <param name="compiledJson">Compiled n8n workflow JSON.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Result with n8n workflow ID.</returns>
    Task<N8nWorkflowResult> CreateWorkflowAsync(
        string name,
        string compiledJson,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates an existing workflow in n8n.
    /// </summary>
    /// <param name="n8nWorkflowId">The n8n workflow ID.</param>
    /// <param name="name">Updated workflow name.</param>
    /// <param name="compiledJson">Updated compiled n8n workflow JSON.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Result indicating success or failure.</returns>
    Task<N8nWorkflowResult> UpdateWorkflowAsync(
        string n8nWorkflowId,
        string name,
        string compiledJson,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Activates a workflow in n8n for execution.
    /// </summary>
    /// <param name="n8nWorkflowId">The n8n workflow ID.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task ActivateWorkflowAsync(string n8nWorkflowId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Deactivates a workflow in n8n.
    /// </summary>
    /// <param name="n8nWorkflowId">The n8n workflow ID.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task DeactivateWorkflowAsync(string n8nWorkflowId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Executes a workflow in n8n.
    /// </summary>
    /// <param name="n8nWorkflowId">The n8n workflow ID.</param>
    /// <param name="inputsJson">Optional JSON inputs for the workflow.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Result with n8n execution ID.</returns>
    Task<N8nExecutionResult> ExecuteWorkflowAsync(
        string n8nWorkflowId,
        string? inputsJson = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the status of a workflow execution.
    /// </summary>
    /// <param name="n8nExecutionId">The n8n execution ID.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Execution status.</returns>
    Task<N8nExecutionStatus> GetExecutionStatusAsync(
        string n8nExecutionId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Resumes a paused execution (after approval).
    /// </summary>
    /// <param name="n8nExecutionId">The n8n execution ID.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task ResumeExecutionAsync(string n8nExecutionId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes a workflow from n8n.
    /// </summary>
    /// <param name="n8nWorkflowId">The n8n workflow ID.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task DeleteWorkflowAsync(string n8nWorkflowId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Builds a workflow using the Flow Builder system workflow.
    /// This is the preferred method for creating workflows from specs.
    /// </summary>
    /// <param name="specJson">The workflow spec JSON.</param>
    /// <param name="tenantId">The tenant/owner ID.</param>
    /// <param name="requestedBy">User ID that requested the build.</param>
    /// <param name="idempotencyKey">Optional idempotency key.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Result with workflow and spec IDs.</returns>
    Task<FlowBuilderResult> BuildWorkflowAsync(
        string specJson,
        Guid tenantId,
        string requestedBy,
        string? idempotencyKey = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Runs a workflow using the Flow Runner system workflow.
    /// This is the preferred method for executing workflows.
    /// </summary>
    /// <param name="workflowId">The n8n workflow ID to execute.</param>
    /// <param name="inputsJson">Optional JSON inputs for the workflow.</param>
    /// <param name="tenantId">The tenant/owner ID.</param>
    /// <param name="requestedBy">User ID that requested the run.</param>
    /// <param name="waitForCompletion">Whether to wait for the workflow to complete.</param>
    /// <param name="timeoutSeconds">Timeout for waiting (default 300 seconds).</param>
    /// <param name="idempotencyKey">Optional idempotency key.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Result with execution status and output.</returns>
    Task<FlowRunnerResult> RunWorkflowAsync(
        string workflowId,
        string? inputsJson,
        Guid tenantId,
        string requestedBy,
        bool waitForCompletion = true,
        int timeoutSeconds = 300,
        string? idempotencyKey = null,
        CancellationToken cancellationToken = default);
}
