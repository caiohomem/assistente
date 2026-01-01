using AssistenteExecutivo.Domain.DomainEvents;
using AssistenteExecutivo.Domain.Enums;
using AssistenteExecutivo.Domain.Exceptions;
using AssistenteExecutivo.Domain.Interfaces;

namespace AssistenteExecutivo.Domain.Entities;

/// <summary>
/// Entity representing a single execution instance of a workflow.
/// Tracks the status, inputs, outputs, and audit trail of the execution.
/// </summary>
public class WorkflowExecution
{
    private readonly List<IDomainEvent> _domainEvents = new();

    private WorkflowExecution() { } // EF Core

    public WorkflowExecution(
        Guid executionId,
        Guid workflowId,
        Guid ownerUserId,
        int specVersionUsed,
        string? inputJson,
        IClock clock)
    {
        if (executionId == Guid.Empty)
            throw new DomainException("Domain:ExecutionIdObrigatorio");

        if (workflowId == Guid.Empty)
            throw new DomainException("Domain:WorkflowIdObrigatorio");

        if (ownerUserId == Guid.Empty)
            throw new DomainException("Domain:OwnerUserIdObrigatorio");

        ExecutionId = executionId;
        WorkflowId = workflowId;
        OwnerUserId = ownerUserId;
        SpecVersionUsed = specVersionUsed;
        InputJson = inputJson;
        Status = WorkflowExecutionStatus.Pending;
        StartedAt = clock.UtcNow;
    }

    public Guid ExecutionId { get; private set; }
    public Guid WorkflowId { get; private set; }
    public Guid OwnerUserId { get; private set; }
    public int SpecVersionUsed { get; private set; }
    public string? InputJson { get; private set; }
    public string? OutputJson { get; private set; }
    public WorkflowExecutionStatus Status { get; private set; }
    public string? N8nExecutionId { get; private set; }
    public string? ErrorMessage { get; private set; }
    public int? CurrentStepIndex { get; private set; }
    public string? IdempotencyKey { get; private set; }
    public DateTime StartedAt { get; private set; }
    public DateTime? CompletedAt { get; private set; }

    public IReadOnlyCollection<IDomainEvent> DomainEvents => _domainEvents.AsReadOnly();

    /// <summary>
    /// Factory method to start a new workflow execution.
    /// </summary>
    public static WorkflowExecution Start(
        Guid executionId,
        Guid workflowId,
        Guid ownerUserId,
        int specVersion,
        string? inputJson,
        IClock clock)
    {
        var execution = new WorkflowExecution(
            executionId, workflowId, ownerUserId, specVersion, inputJson, clock);

        execution._domainEvents.Add(new WorkflowExecutionStarted(
            executionId, workflowId, clock.UtcNow));

        return execution;
    }

    /// <summary>
    /// Marks the execution as running in n8n.
    /// </summary>
    public void MarkRunning(string n8nExecutionId)
    {
        if (string.IsNullOrWhiteSpace(n8nExecutionId))
            throw new DomainException("Domain:N8nExecutionIdObrigatorio");

        N8nExecutionId = n8nExecutionId;
        Status = WorkflowExecutionStatus.Running;
    }

    /// <summary>
    /// Updates the current step being executed.
    /// </summary>
    public void UpdateCurrentStep(int stepIndex)
    {
        CurrentStepIndex = stepIndex;
    }

    /// <summary>
    /// Pauses execution waiting for user approval on a step.
    /// </summary>
    public void RequestApproval(int stepIndex, IClock clock)
    {
        if (Status != WorkflowExecutionStatus.Running)
            throw new DomainException("Domain:ExecutionDeveEstarRodandoParaSolicitarAprovacao");

        Status = WorkflowExecutionStatus.WaitingApproval;
        CurrentStepIndex = stepIndex;

        _domainEvents.Add(new WorkflowApprovalRequired(
            ExecutionId, WorkflowId, stepIndex, clock.UtcNow));
    }

    /// <summary>
    /// Approves the pending step and resumes execution.
    /// </summary>
    public void ApproveStep(Guid approvedBy, IClock clock)
    {
        if (Status != WorkflowExecutionStatus.WaitingApproval)
            throw new DomainException("Domain:ExecutionNaoAguardandoAprovacao");

        Status = WorkflowExecutionStatus.Running;

        _domainEvents.Add(new WorkflowStepApproved(
            ExecutionId, CurrentStepIndex ?? 0, approvedBy, clock.UtcNow));
    }

    /// <summary>
    /// Marks the execution as completed successfully.
    /// </summary>
    public void Complete(string? outputJson, IClock clock)
    {
        Status = WorkflowExecutionStatus.Completed;
        OutputJson = outputJson;
        CompletedAt = clock.UtcNow;

        _domainEvents.Add(new WorkflowExecutionCompleted(
            ExecutionId, WorkflowId, clock.UtcNow));
    }

    /// <summary>
    /// Marks the execution as failed with an error.
    /// </summary>
    public void Fail(string errorMessage, IClock clock)
    {
        if (string.IsNullOrWhiteSpace(errorMessage))
            errorMessage = "Unknown error";

        Status = WorkflowExecutionStatus.Failed;
        ErrorMessage = errorMessage;
        CompletedAt = clock.UtcNow;

        _domainEvents.Add(new WorkflowExecutionFailed(
            ExecutionId, WorkflowId, errorMessage, clock.UtcNow));
    }

    /// <summary>
    /// Cancels the execution.
    /// </summary>
    public void Cancel(IClock clock)
    {
        if (Status == WorkflowExecutionStatus.Completed || Status == WorkflowExecutionStatus.Failed)
            throw new DomainException("Domain:ExecutionJaFinalizadaNaoPodeSerCancelada");

        Status = WorkflowExecutionStatus.Cancelled;
        CompletedAt = clock.UtcNow;
    }

    /// <summary>
    /// Clears all domain events after publishing.
    /// </summary>
    public void ClearDomainEvents()
    {
        _domainEvents.Clear();
    }

    /// <summary>
    /// Sets the idempotency key for duplicate detection.
    /// </summary>
    public void SetIdempotencyKey(string? idempotencyKey)
    {
        IdempotencyKey = idempotencyKey;
    }

    /// <summary>
    /// Sets the n8n execution ID.
    /// </summary>
    public void SetN8nExecutionId(string n8nExecutionId)
    {
        N8nExecutionId = n8nExecutionId;
    }

    /// <summary>
    /// Updates the execution status.
    /// </summary>
    public void UpdateStatus(WorkflowExecutionStatus status)
    {
        Status = status;
    }

    /// <summary>
    /// Marks the execution as completed successfully with a specific completion time.
    /// </summary>
    public void Complete(string? outputJson, DateTime? completedAt)
    {
        Status = WorkflowExecutionStatus.Completed;
        OutputJson = outputJson;
        CompletedAt = completedAt ?? DateTime.UtcNow;
    }

    /// <summary>
    /// Marks the execution as failed with an error and specific completion time.
    /// </summary>
    public void Fail(string errorMessage, DateTime? completedAt)
    {
        if (string.IsNullOrWhiteSpace(errorMessage))
            errorMessage = "Unknown error";

        Status = WorkflowExecutionStatus.Failed;
        ErrorMessage = errorMessage;
        CompletedAt = completedAt ?? DateTime.UtcNow;
    }

    /// <summary>
    /// Alternative constructor for creating executions with a specific start time.
    /// </summary>
    public WorkflowExecution(
        Guid workflowId,
        Guid ownerUserId,
        int specVersionUsed,
        string? inputJson,
        DateTime startedAt)
    {
        ExecutionId = Guid.NewGuid();
        WorkflowId = workflowId;
        OwnerUserId = ownerUserId;
        SpecVersionUsed = specVersionUsed;
        InputJson = inputJson;
        Status = WorkflowExecutionStatus.Running;
        StartedAt = startedAt;
    }
}
