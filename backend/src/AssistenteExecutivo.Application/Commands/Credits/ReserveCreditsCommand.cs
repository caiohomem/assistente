using MediatR;

namespace AssistenteExecutivo.Application.Commands.Credits;

public class ReserveCreditsCommand : IRequest<ReserveCreditsResult>
{
    public Guid OwnerUserId { get; set; }
    public decimal Amount { get; set; }
    public string IdempotencyKey { get; set; } = string.Empty;
    public string Purpose { get; set; } = string.Empty;
}

public class ReserveCreditsResult
{
    public Guid OwnerUserId { get; set; }
    public decimal NewBalance { get; set; }
    public Guid TransactionId { get; set; }
    public bool WasIdempotent { get; set; }
}






