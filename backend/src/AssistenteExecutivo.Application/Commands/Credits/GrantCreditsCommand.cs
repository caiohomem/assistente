using MediatR;

namespace AssistenteExecutivo.Application.Commands.Credits;

public class GrantCreditsCommand : IRequest<GrantCreditsResult>
{
    public Guid OwnerUserId { get; set; }
    public decimal Amount { get; set; }
    public string? Reason { get; set; }
}

public class GrantCreditsResult
{
    public Guid OwnerUserId { get; set; }
    public decimal NewBalance { get; set; }
    public Guid TransactionId { get; set; }
}






