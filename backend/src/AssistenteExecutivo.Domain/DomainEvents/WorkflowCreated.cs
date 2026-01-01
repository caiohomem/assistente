namespace AssistenteExecutivo.Domain.DomainEvents;

public record WorkflowCreated(
    Guid WorkflowId,
    Guid OwnerUserId,
    string Name,
    DateTime OccurredAt) : IDomainEvent;
