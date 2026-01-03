using AssistenteExecutivo.Domain.Enums;

namespace AssistenteExecutivo.Domain.DomainEvents;

public record PayoutRequested(
    Guid EscrowAccountId,
    Guid TransactionId,
    Guid PartyId,
    decimal Amount,
    string Currency,
    PayoutApprovalType ApprovalType,
    DateTime OccurredAt) : IDomainEvent;
