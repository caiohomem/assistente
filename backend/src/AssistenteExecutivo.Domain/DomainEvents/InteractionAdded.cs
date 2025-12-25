using AssistenteExecutivo.Domain.Enums;

namespace AssistenteExecutivo.Domain.DomainEvents;

public record InteractionAdded(
    Guid ContactId,
    Guid InteractionId,
    InteractionType InteractionType,
    DateTime OccurredAt) : IDomainEvent;

