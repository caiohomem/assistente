namespace AssistenteExecutivo.Domain.DomainEvents;

public record WorkflowApprovalRequired(
    Guid ExecutionId,
    Guid WorkflowId,
    int StepIndex,
    DateTime OccurredAt) : IDomainEvent;
