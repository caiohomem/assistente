using AssistenteExecutivo.Domain.ValueObjects;

namespace AssistenteExecutivo.Domain.DomainEvents;

public record CreditsGranted(
    Guid OwnerUserId,
    CreditAmount Amount,
    DateTime OccurredAt) : IDomainEvent;

