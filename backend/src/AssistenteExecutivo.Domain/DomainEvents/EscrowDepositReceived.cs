namespace AssistenteExecutivo.Domain.DomainEvents;

public record EscrowDepositReceived(
    Guid EscrowAccountId,
    Guid TransactionId,
    decimal Amount,
    string Currency,
    DateTime OccurredAt) : IDomainEvent;
