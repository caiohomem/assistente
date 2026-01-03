namespace AssistenteExecutivo.Domain.DomainEvents;

public record PayoutExecuted(
    Guid EscrowAccountId,
    Guid TransactionId,
    decimal Amount,
    string Currency,
    string? StripeTransferId,
    DateTime OccurredAt) : IDomainEvent;
