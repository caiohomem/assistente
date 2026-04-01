using AssistenteExecutivo.Domain.Enums;

namespace AssistenteExecutivo.Application.DTOs;

public class EscrowTransactionDto
{
    public Guid TransactionId { get; set; }
    public Guid EscrowAccountId { get; set; }
    public Guid? PartyId { get; set; }
    public EscrowTransactionType Type { get; set; }
    public decimal Amount { get; set; }
    public string Currency { get; set; } = "BRL";
    public string? Description { get; set; }
    public EscrowTransactionStatus Status { get; set; }
    public PayoutApprovalType? ApprovalType { get; set; }
    public Guid? ApprovedBy { get; set; }
    public DateTime? ApprovedAt { get; set; }
    public Guid? RejectedBy { get; set; }
    public string? RejectionReason { get; set; }
    public string? DisputeReason { get; set; }
    public string? StripePaymentIntentId { get; set; }
    public string? StripeTransferId { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}
