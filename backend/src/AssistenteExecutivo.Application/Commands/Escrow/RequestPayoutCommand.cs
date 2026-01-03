using AssistenteExecutivo.Domain.Enums;
using MediatR;

namespace AssistenteExecutivo.Application.Commands.Escrow;

public class RequestPayoutCommand : IRequest<Guid>
{
    public Guid EscrowAccountId { get; set; }
    public Guid TransactionId { get; set; }
    public Guid? PartyId { get; set; }
    public decimal Amount { get; set; }
    public string Currency { get; set; } = "BRL";
    public string? Description { get; set; }
    public PayoutApprovalType ApprovalType { get; set; } = PayoutApprovalType.ApprovalRequired;
    public string? IdempotencyKey { get; set; }
    public Guid RequestedBy { get; set; }
}
