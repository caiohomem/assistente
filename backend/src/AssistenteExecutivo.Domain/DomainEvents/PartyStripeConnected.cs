namespace AssistenteExecutivo.Domain.DomainEvents;

public record PartyStripeConnected(
    Guid AgreementId,
    Guid PartyId,
    string StripeAccountId,
    DateTime OccurredAt) : IDomainEvent;
