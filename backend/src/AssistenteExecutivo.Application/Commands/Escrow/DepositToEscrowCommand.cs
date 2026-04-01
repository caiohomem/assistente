using AssistenteExecutivo.Application.DTOs;
using MediatR;

namespace AssistenteExecutivo.Application.Commands.Escrow;

public class DepositToEscrowCommand : IRequest<EscrowDepositResult>
{
    public Guid EscrowAccountId { get; set; }
    public Guid TransactionId { get; set; }
    public decimal Amount { get; set; }
    public string Currency { get; set; } = "BRL";
    public string? Description { get; set; }
    public string? PaymentIntentId { get; set; }
    public string? IdempotencyKey { get; set; }
    public Guid RequestedBy { get; set; }
}
