namespace AssistenteExecutivo.Domain.DomainEvents;

public record AgreementCreated(
    Guid AgreementId,
    Guid OwnerUserId,
    string Title,
    decimal TotalValue,
    string Currency,
    DateTime OccurredAt) : IDomainEvent;
