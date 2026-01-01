namespace AssistenteExecutivo.Domain.DomainEvents;

public record WorkflowSpecUpdated(
    Guid WorkflowId,
    int NewVersion,
    DateTime OccurredAt) : IDomainEvent;
