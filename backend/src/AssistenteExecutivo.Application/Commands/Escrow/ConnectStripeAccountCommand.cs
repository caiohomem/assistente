using MediatR;

namespace AssistenteExecutivo.Application.Commands.Escrow;

public class ConnectStripeAccountCommand : IRequest<Unit>
{
    public Guid EscrowAccountId { get; set; }
    public Guid OwnerUserId { get; set; }
    public string AuthorizationCode { get; set; } = string.Empty;
}
