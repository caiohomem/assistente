namespace AssistenteExecutivo.Domain.DomainEvents;

public record AgreementDisputed(
    Guid AgreementId,
    string Reason,
    DateTime OccurredAt) : IDomainEvent;
