using AssistenteExecutivo.Domain.Enums;

namespace AssistenteExecutivo.Domain.DomainEvents;

public record PayoutApproved(
    Guid EscrowAccountId,
    Guid TransactionId,
    Guid ApprovedBy,
    PayoutApprovalType ApprovalType,
    DateTime OccurredAt) : IDomainEvent;
