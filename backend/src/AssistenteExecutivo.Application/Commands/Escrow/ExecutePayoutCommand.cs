using MediatR;

namespace AssistenteExecutivo.Application.Commands.Escrow;

public class ExecutePayoutCommand : IRequest<Unit>
{
    public Guid EscrowAccountId { get; set; }
    public Guid TransactionId { get; set; }
    public string? StripeTransferId { get; set; }
    public Guid PerformedBy { get; set; }
}
