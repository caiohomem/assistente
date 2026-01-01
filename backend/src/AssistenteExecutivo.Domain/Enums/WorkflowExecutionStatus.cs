namespace AssistenteExecutivo.Domain.Enums;

/// <summary>
/// Defines the status of a workflow execution instance.
/// </summary>
public enum WorkflowExecutionStatus
{
    /// <summary>
    /// Execution is queued and waiting to start.
    /// </summary>
    Pending = 1,

    /// <summary>
    /// Execution is currently running.
    /// </summary>
    Running = 2,

    /// <summary>
    /// Execution is paused waiting for user approval.
    /// </summary>
    WaitingApproval = 3,

    /// <summary>
    /// Execution completed successfully.
    /// </summary>
    Completed = 4,

    /// <summary>
    /// Execution failed with an error.
    /// </summary>
    Failed = 5,

    /// <summary>
    /// Execution was cancelled by user.
    /// </summary>
    Cancelled = 6
}
