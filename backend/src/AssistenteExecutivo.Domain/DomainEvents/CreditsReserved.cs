using AssistenteExecutivo.Domain.ValueObjects;

namespace AssistenteExecutivo.Domain.DomainEvents;

public record CreditsReserved(
    Guid OwnerUserId,
    CreditAmount Amount,
    string Purpose,
    DateTime OccurredAt) : IDomainEvent;

