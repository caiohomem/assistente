using MediatR;

namespace AssistenteExecutivo.Application.Commands.Credits;

public class RefundCreditsCommand : IRequest<RefundCreditsResult>
{
    public Guid OwnerUserId { get; set; }
    public decimal Amount { get; set; }
    public string IdempotencyKey { get; set; } = string.Empty;
    public string Purpose { get; set; } = string.Empty;
}

public class RefundCreditsResult
{
    public Guid OwnerUserId { get; set; }
    public decimal NewBalance { get; set; }
    public Guid TransactionId { get; set; }
    public bool WasIdempotent { get; set; }
}














