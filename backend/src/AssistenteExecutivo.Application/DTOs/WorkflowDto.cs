using AssistenteExecutivo.Domain.Enums;

namespace AssistenteExecutivo.Application.DTOs;

/// <summary>
/// DTO for Workflow entity.
/// </summary>
public class WorkflowDto
{
    public Guid WorkflowId { get; set; }
    public Guid OwnerUserId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public WorkflowTriggerDto Trigger { get; set; } = new();
    public string SpecJson { get; set; } = string.Empty;
    public int SpecVersion { get; set; }
    public string? N8nWorkflowId { get; set; }
    public WorkflowStatus Status { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}

/// <summary>
/// DTO for WorkflowTrigger value object.
/// </summary>
public class WorkflowTriggerDto
{
    public TriggerType Type { get; set; }
    public string? CronExpression { get; set; }
    public string? EventName { get; set; }
    public string? ConfigJson { get; set; }
}

/// <summary>
/// DTO for WorkflowExecution entity.
/// </summary>
public class WorkflowExecutionDto
{
    public Guid ExecutionId { get; set; }
    public Guid WorkflowId { get; set; }
    public Guid OwnerUserId { get; set; }
    public int SpecVersionUsed { get; set; }
    public string? InputJson { get; set; }
    public string? OutputJson { get; set; }
    public WorkflowExecutionStatus Status { get; set; }
    public string? N8nExecutionId { get; set; }
    public string? ErrorMessage { get; set; }
    public int? CurrentStepIndex { get; set; }
    public DateTime StartedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
}

/// <summary>
/// Summary DTO for listing workflows.
/// </summary>
public class WorkflowSummaryDto
{
    public Guid WorkflowId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public TriggerType TriggerType { get; set; }
    public WorkflowStatus Status { get; set; }
    public int SpecVersion { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}

/// <summary>
/// Summary DTO for listing executions.
/// </summary>
public class WorkflowExecutionSummaryDto
{
    public Guid ExecutionId { get; set; }
    public Guid WorkflowId { get; set; }
    public string WorkflowName { get; set; } = string.Empty;
    public WorkflowExecutionStatus Status { get; set; }
    public DateTime StartedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
}
