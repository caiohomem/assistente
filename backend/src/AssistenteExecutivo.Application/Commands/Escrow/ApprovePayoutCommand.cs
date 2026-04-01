using MediatR;

namespace AssistenteExecutivo.Application.Commands.Escrow;

public class ApprovePayoutCommand : IRequest<Unit>
{
    public Guid EscrowAccountId { get; set; }
    public Guid TransactionId { get; set; }
    public Guid ApprovedBy { get; set; }
}
