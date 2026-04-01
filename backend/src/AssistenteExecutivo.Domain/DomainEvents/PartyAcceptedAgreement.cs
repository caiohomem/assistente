namespace AssistenteExecutivo.Domain.DomainEvents;

public record PartyAcceptedAgreement(
    Guid AgreementId,
    Guid PartyId,
    DateTime OccurredAt) : IDomainEvent;
