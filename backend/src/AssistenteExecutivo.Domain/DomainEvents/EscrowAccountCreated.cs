namespace AssistenteExecutivo.Domain.DomainEvents;

public record EscrowAccountCreated(
    Guid EscrowAccountId,
    Guid AgreementId,
    Guid OwnerUserId,
    DateTime OccurredAt) : IDomainEvent;
