namespace AssistenteExecutivo.Domain.DomainEvents;

public record WorkflowExecutionFailed(
    Guid ExecutionId,
    Guid WorkflowId,
    string ErrorMessage,
    DateTime OccurredAt) : IDomainEvent;
