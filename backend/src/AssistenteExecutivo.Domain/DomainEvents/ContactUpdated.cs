namespace AssistenteExecutivo.Domain.DomainEvents;

public record ContactUpdated(
    Guid ContactId,
    DateTime OccurredAt) : IDomainEvent;

