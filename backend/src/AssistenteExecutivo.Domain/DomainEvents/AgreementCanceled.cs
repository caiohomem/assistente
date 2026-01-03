namespace AssistenteExecutivo.Domain.DomainEvents;

public record AgreementCanceled(
    Guid AgreementId,
    string Reason,
    DateTime OccurredAt) : IDomainEvent;
