using AssistenteExecutivo.Domain.ValueObjects;

namespace AssistenteExecutivo.Domain.DomainEvents;

public record CreditsRefunded(
    Guid OwnerUserId,
    CreditAmount Amount,
    string Purpose,
    DateTime OccurredAt) : IDomainEvent;

