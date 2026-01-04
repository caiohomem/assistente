using MediatR;

namespace AssistenteExecutivo.Application.Commands.Commission;

public class ConnectPartyStripeAccountCommand : IRequest<Unit>
{
    public Guid AgreementId { get; set; }
    public Guid PartyId { get; set; }
    public string AuthorizationCodeOrAccountId { get; set; } = string.Empty;
    public Guid RequestedBy { get; set; }
}
