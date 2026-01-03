using MediatR;

namespace AssistenteExecutivo.Application.Commands.Commission;

public class AcceptAgreementAsPartyCommand : IRequest<Unit>
{
    public Guid AgreementId { get; set; }
    public Guid PartyId { get; set; }
    public Guid? ActingUserId { get; set; }
}
