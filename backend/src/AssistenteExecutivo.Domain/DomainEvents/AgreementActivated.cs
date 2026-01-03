namespace AssistenteExecutivo.Domain.DomainEvents;

public record AgreementActivated(
    Guid AgreementId,
    DateTime OccurredAt) : IDomainEvent;
