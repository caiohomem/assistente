using AssistenteExecutivo.Domain.Enums;

namespace AssistenteExecutivo.Domain.DomainEvents;

public record PartyAddedToAgreement(
    Guid AgreementId,
    Guid PartyId,
    string PartyName,
    decimal SplitPercentage,
    PartyRole Role,
    DateTime OccurredAt) : IDomainEvent;
