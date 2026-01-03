using AssistenteExecutivo.Domain.Enums;
using AssistenteExecutivo.Domain.Exceptions;
using AssistenteExecutivo.Domain.Interfaces;
using AssistenteExecutivo.Domain.ValueObjects;

namespace AssistenteExecutivo.Domain.Entities;

public class EscrowTransaction
{
    private EscrowTransaction() { } // EF Core

    private EscrowTransaction(
        Guid transactionId,
        Guid escrowAccountId,
        Guid? partyId,
        EscrowTransactionType type,
        Money amount,
        string? description,
        EscrowTransactionStatus status,
        PayoutApprovalType? approvalType,
        string? stripePaymentIntentId,
        string? stripeTransferId,
        string? idempotencyKey,
        IClock clock)
    {
        if (transactionId == Guid.Empty)
            throw new DomainException("Domain:TransactionIdObrigatorio");

        if (escrowAccountId == Guid.Empty)
            throw new DomainException("Domain:EscrowAccountIdObrigatorio");

        if (amount == null)
            throw new DomainException("Domain:ValorTransacaoObrigatorio");

        TransactionId = transactionId;
        EscrowAccountId = escrowAccountId;
        PartyId = partyId;
        Type = type;
        Amount = amount;
        Description = string.IsNullOrWhiteSpace(description) ? null : description.Trim();
        Status = status;
        ApprovalType = approvalType;
        StripePaymentIntentId = stripePaymentIntentId;
        StripeTransferId = stripeTransferId;
        IdempotencyKey = string.IsNullOrWhiteSpace(idempotencyKey) ? null : idempotencyKey.Trim();
        CreatedAt = clock.UtcNow;
        UpdatedAt = clock.UtcNow;
    }

    public Guid TransactionId { get; private set; }
    public Guid EscrowAccountId { get; private set; }
    public Guid? PartyId { get; private set; }
    public EscrowTransactionType Type { get; private set; }
    public Money Amount { get; private set; } = null!;
    public string? Description { get; private set; }
    public EscrowTransactionStatus Status { get; private set; }
    public PayoutApprovalType? ApprovalType { get; private set; }
    public Guid? ApprovedBy { get; private set; }
    public DateTime? ApprovedAt { get; private set; }
    public Guid? RejectedBy { get; private set; }
    public string? RejectionReason { get; private set; }
    public string? DisputeReason { get; private set; }
    public string? StripePaymentIntentId { get; private set; }
    public string? StripeTransferId { get; private set; }
    public string? IdempotencyKey { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime UpdatedAt { get; private set; }

    public static EscrowTransaction CreateDeposit(
        Guid transactionId,
        Guid escrowAccountId,
        Money amount,
        string? description,
        EscrowTransactionStatus status,
        string? paymentIntentId,
        string? idempotencyKey,
        IClock clock)
    {
        return new EscrowTransaction(
            transactionId,
            escrowAccountId,
            null,
            EscrowTransactionType.Deposit,
            amount,
            description,
            status,
            null,
            paymentIntentId,
            null,
            idempotencyKey,
            clock);
    }

    public static EscrowTransaction CreatePayout(
        Guid transactionId,
        Guid escrowAccountId,
        Guid? partyId,
        Money amount,
        string? description,
        PayoutApprovalType approvalType,
        string? idempotencyKey,
        IClock clock)
    {
        return new EscrowTransaction(
            transactionId,
            escrowAccountId,
            partyId,
            EscrowTransactionType.Payout,
            amount,
            description,
            EscrowTransactionStatus.Pending,
            approvalType,
            null,
            null,
            idempotencyKey,
            clock);
    }

    public static EscrowTransaction CreateRefund(
        Guid transactionId,
        Guid escrowAccountId,
        Money amount,
        string? description,
        string? stripeTransferId,
        IClock clock)
    {
        return new EscrowTransaction(
            transactionId,
            escrowAccountId,
            null,
            EscrowTransactionType.Refund,
            amount,
            description,
            EscrowTransactionStatus.Completed,
            null,
            null,
            stripeTransferId,
            null,
            clock);
    }

    internal void MarkApproved(Guid approvedBy, IClock clock)
    {
        if (Status != EscrowTransactionStatus.Pending)
            throw new DomainException("Domain:TransacaoNaoEstaPendente");

        ApprovedBy = approvedBy;
        ApprovedAt = clock.UtcNow;
        Status = EscrowTransactionStatus.Approved;
        UpdatedAt = clock.UtcNow;
    }

    internal void MarkRejected(Guid rejectedBy, string reason, IClock clock)
    {
        if (Status != EscrowTransactionStatus.Pending && Status != EscrowTransactionStatus.Approved)
            throw new DomainException("Domain:TransacaoNaoPodeSerRejeitada");

        RejectedBy = rejectedBy;
        RejectionReason = string.IsNullOrWhiteSpace(reason) ? "Sem motivo" : reason.Trim();
        Status = EscrowTransactionStatus.Rejected;
        UpdatedAt = clock.UtcNow;
    }

    internal void MarkDisputed(string reason, IClock clock)
    {
        DisputeReason = string.IsNullOrWhiteSpace(reason) ? "Sem motivo" : reason.Trim();
        Status = EscrowTransactionStatus.Disputed;
        UpdatedAt = clock.UtcNow;
    }

    internal void MarkCompleted(string? stripeTransferId, IClock clock)
    {
        if (Status != EscrowTransactionStatus.Approved && Status != EscrowTransactionStatus.Pending)
            throw new DomainException("Domain:TransacaoNaoPodeSerConcluida");

        StripeTransferId = string.IsNullOrWhiteSpace(stripeTransferId) ? null : stripeTransferId.Trim();
        Status = EscrowTransactionStatus.Completed;
        UpdatedAt = clock.UtcNow;
    }

    internal void UpdatePaymentIntentReference(string paymentIntentId)
    {
        if (string.IsNullOrWhiteSpace(paymentIntentId))
            throw new DomainException("Domain:PaymentIntentObrigatorio");

        StripePaymentIntentId = paymentIntentId.Trim();
    }
}
