namespace AssistenteExecutivo.Domain.DomainEvents;

public record AgreementGeneratedFromNegotiation(
    Guid SessionId,
    Guid AgreementId,
    DateTime OccurredAt) : IDomainEvent;
