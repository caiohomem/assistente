using AssistenteExecutivo.Application.DTOs;
using MediatR;

namespace AssistenteExecutivo.Application.Queries.Negotiations;

public class ListProposalsBySessionQuery : IRequest<List<NegotiationProposalDto>>
{
    public Guid SessionId { get; set; }
    public Guid RequestingUserId { get; set; }
}
