namespace AssistenteExecutivo.Domain.Enums;

/// <summary>
/// Defines the lifecycle status of a workflow definition.
/// </summary>
public enum WorkflowStatus
{
    /// <summary>
    /// Workflow is being designed and not yet active.
    /// </summary>
    Draft = 1,

    /// <summary>
    /// Workflow is active and can be executed.
    /// </summary>
    Active = 2,

    /// <summary>
    /// Workflow is temporarily paused.
    /// </summary>
    Paused = 3,

    /// <summary>
    /// Workflow is archived and no longer in use.
    /// </summary>
    Archived = 4
}
