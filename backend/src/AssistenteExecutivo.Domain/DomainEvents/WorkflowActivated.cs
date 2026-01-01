namespace AssistenteExecutivo.Domain.DomainEvents;

public record WorkflowActivated(
    Guid WorkflowId,
    DateTime OccurredAt) : IDomainEvent;
