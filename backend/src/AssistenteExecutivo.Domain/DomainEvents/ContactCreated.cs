namespace AssistenteExecutivo.Domain.DomainEvents;

public record ContactCreated(
    Guid ContactId,
    Guid OwnerUserId,
    string Source,
    DateTime OccurredAt) : IDomainEvent;

