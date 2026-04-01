using AssistenteExecutivo.Domain.Enums;
using MediatR;

namespace AssistenteExecutivo.Application.Commands.Negotiations;

public class SubmitProposalCommand : IRequest<Guid>
{
    public Guid SessionId { get; set; }
    public Guid ProposalId { get; set; }
    public Guid? PartyId { get; set; }
    public ProposalSource Source { get; set; } = ProposalSource.Party;
    public string Content { get; set; } = string.Empty;
}
