namespace AssistenteExecutivo.Domain.DomainEvents;

public record WorkflowExecutionStarted(
    Guid ExecutionId,
    Guid WorkflowId,
    DateTime OccurredAt) : IDomainEvent;
