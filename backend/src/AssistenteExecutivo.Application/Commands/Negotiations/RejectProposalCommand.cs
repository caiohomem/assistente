using MediatR;

namespace AssistenteExecutivo.Application.Commands.Negotiations;

public class RejectProposalCommand : IRequest<Unit>
{
    public Guid SessionId { get; set; }
    public Guid ProposalId { get; set; }
    public Guid? ActingPartyId { get; set; }
    public string Reason { get; set; } = string.Empty;
}
