namespace AssistenteExecutivo.Domain.DomainEvents;

public record WorkflowStepApproved(
    Guid ExecutionId,
    int StepIndex,
    Guid ApprovedBy,
    DateTime OccurredAt) : IDomainEvent;
