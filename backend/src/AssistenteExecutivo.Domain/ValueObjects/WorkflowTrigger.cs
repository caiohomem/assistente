using AssistenteExecutivo.Domain.Enums;

namespace AssistenteExecutivo.Domain.ValueObjects;

/// <summary>
/// Value object representing how a workflow is triggered.
/// </summary>
public class WorkflowTrigger : ValueObject
{
    public TriggerType Type { get; private set; }
    public string? CronExpression { get; private set; }
    public string? EventName { get; private set; }
    public string? ConfigJson { get; private set; }

    private WorkflowTrigger() { } // EF Core

    private WorkflowTrigger(
        TriggerType type,
        string? cronExpression = null,
        string? eventName = null,
        string? configJson = null)
    {
        Type = type;
        CronExpression = cronExpression;
        EventName = eventName;
        ConfigJson = configJson;
    }

    /// <summary>
    /// Creates a manual trigger (user-initiated).
    /// </summary>
    public static WorkflowTrigger Manual() =>
        new(TriggerType.Manual);

    /// <summary>
    /// Creates a scheduled trigger based on cron expression.
    /// </summary>
    /// <param name="cronExpression">Cron expression (e.g., "0 9 * * MON-FRI")</param>
    public static WorkflowTrigger Scheduled(string cronExpression)
    {
        if (string.IsNullOrWhiteSpace(cronExpression))
            throw new ArgumentException("Cron expression is required for scheduled triggers", nameof(cronExpression));

        return new WorkflowTrigger(TriggerType.Scheduled, cronExpression: cronExpression);
    }

    /// <summary>
    /// Creates an event-based trigger.
    /// </summary>
    /// <param name="eventName">Event name (e.g., "contact.created", "note.created")</param>
    /// <param name="configJson">Optional JSON configuration for event filtering</param>
    public static WorkflowTrigger EventBased(string eventName, string? configJson = null)
    {
        if (string.IsNullOrWhiteSpace(eventName))
            throw new ArgumentException("Event name is required for event-based triggers", nameof(eventName));

        return new WorkflowTrigger(TriggerType.EventBased, eventName: eventName, configJson: configJson);
    }

    protected override IEnumerable<object> GetEqualityComponents()
    {
        yield return Type;
        yield return CronExpression ?? string.Empty;
        yield return EventName ?? string.Empty;
        yield return ConfigJson ?? string.Empty;
    }
}
