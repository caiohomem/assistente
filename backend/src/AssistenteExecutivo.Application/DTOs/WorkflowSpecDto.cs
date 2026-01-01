using AssistenteExecutivo.Domain.Enums;

namespace AssistenteExecutivo.Application.DTOs;

/// <summary>
/// Root DTO representing the complete workflow specification.
/// This is the contract that the LLM generates and the compiler transforms to n8n format.
/// </summary>
public class WorkflowSpecDto
{
    public string Version { get; set; } = "1.0";
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public TriggerSpecDto Trigger { get; set; } = new();
    public Dictionary<string, VariableSpecDto> Variables { get; set; } = new();
    public List<StepSpecDto> Steps { get; set; } = new();
}

/// <summary>
/// Trigger configuration for the workflow.
/// </summary>
public class TriggerSpecDto
{
    public TriggerType Type { get; set; } = TriggerType.Manual;
    public string? CronExpression { get; set; }
    public string? EventName { get; set; }
    public Dictionary<string, object>? Config { get; set; }
}

/// <summary>
/// Variable definition for workflow context.
/// </summary>
public class VariableSpecDto
{
    public string Type { get; set; } = "string";
    public object? DefaultValue { get; set; }
    public string? Description { get; set; }
    public bool Required { get; set; }
}

/// <summary>
/// A single step in the workflow.
/// </summary>
public class StepSpecDto
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public StepType Type { get; set; }
    public ActionSpecDto? Action { get; set; }
    public ConditionSpecDto? Condition { get; set; }
    public List<string>? OnSuccess { get; set; }
    public List<string>? OnFailure { get; set; }
    public bool ApprovalRequired { get; set; }
}

/// <summary>
/// Step type: ACTION or CONDITION.
/// </summary>
public enum StepType
{
    Action,
    Condition
}

/// <summary>
/// Action specification.
/// </summary>
public class ActionSpecDto
{
    public ActionType ActionType { get; set; }
    public Dictionary<string, object> Parameters { get; set; } = new();
    public RetryConfigDto? Retry { get; set; }
}

/// <summary>
/// Condition specification for branching logic.
/// </summary>
public class ConditionSpecDto
{
    public ConditionType ConditionType { get; set; }
    public string LeftOperand { get; set; } = string.Empty;
    public object? RightOperand { get; set; }
    public List<string>? TrueBranch { get; set; }
    public List<string>? FalseBranch { get; set; }
}

/// <summary>
/// Retry configuration for actions.
/// </summary>
public class RetryConfigDto
{
    public int MaxAttempts { get; set; } = 3;
    public int DelaySeconds { get; set; } = 5;
    public bool ExponentialBackoff { get; set; } = true;
}
