namespace AssistenteExecutivo.Domain.DomainEvents;

public record DraftSent(
    Guid DraftId,
    Guid SentBy,
    DateTime OccurredAt) : IDomainEvent;

