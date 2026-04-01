namespace AssistenteExecutivo.Domain.DomainEvents;

public record PayoutRejected(
    Guid EscrowAccountId,
    Guid TransactionId,
    Guid RejectedBy,
    string Reason,
    DateTime OccurredAt) : IDomainEvent;
