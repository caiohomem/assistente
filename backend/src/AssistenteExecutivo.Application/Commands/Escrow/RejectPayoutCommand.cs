using MediatR;

namespace AssistenteExecutivo.Application.Commands.Escrow;

public class RejectPayoutCommand : IRequest<Unit>
{
    public Guid EscrowAccountId { get; set; }
    public Guid TransactionId { get; set; }
    public Guid RejectedBy { get; set; }
    public string Reason { get; set; } = string.Empty;
}
