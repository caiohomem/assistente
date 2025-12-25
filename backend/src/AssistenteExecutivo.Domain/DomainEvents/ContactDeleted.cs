namespace AssistenteExecutivo.Domain.DomainEvents;

public record ContactDeleted(
    Guid ContactId,
    DateTime OccurredAt) : IDomainEvent;

