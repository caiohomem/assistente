namespace AssistenteExecutivo.Domain.DomainEvents;

public record WorkflowExecutionCompleted(
    Guid ExecutionId,
    Guid WorkflowId,
    DateTime OccurredAt) : IDomainEvent;
