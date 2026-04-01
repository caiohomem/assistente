namespace AssistenteExecutivo.Domain.DomainEvents;

public record AgreementCompleted(
    Guid AgreementId,
    DateTime OccurredAt) : IDomainEvent;
