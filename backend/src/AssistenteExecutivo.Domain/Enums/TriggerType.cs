namespace AssistenteExecutivo.Domain.Enums;

/// <summary>
/// Defines how a workflow is triggered.
/// </summary>
public enum TriggerType
{
    /// <summary>
    /// Workflow is triggered manually by user action.
    /// </summary>
    Manual = 1,

    /// <summary>
    /// Workflow is triggered on a schedule (cron expression).
    /// </summary>
    Scheduled = 2,

    /// <summary>
    /// Workflow is triggered by a system event (e.g., contact.created).
    /// </summary>
    EventBased = 3,

    /// <summary>
    /// Workflow is triggered by an HTTP webhook.
    /// </summary>
    Webhook = 4
}
