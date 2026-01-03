using System.Linq;
using AssistenteExecutivo.Application.DTOs;
using AssistenteExecutivo.Application.Interfaces;
using AssistenteExecutivo.Application.Queries.Negotiations;
using AssistenteExecutivo.Domain.Exceptions;
using MediatR;

namespace AssistenteExecutivo.Application.Handlers.Negotiations;

public class ListProposalsBySessionQueryHandler : IRequestHandler<ListProposalsBySessionQuery, List<NegotiationProposalDto>>
{
    private readonly INegotiationSessionRepository _sessionRepository;

    public ListProposalsBySessionQueryHandler(INegotiationSessionRepository sessionRepository)
    {
        _sessionRepository = sessionRepository;
    }

    public async Task<List<NegotiationProposalDto>> Handle(ListProposalsBySessionQuery request, CancellationToken cancellationToken)
    {
        if (request.RequestingUserId == Guid.Empty)
            throw new DomainException("Domain:UsuarioSolicitanteObrigatorio");

        var session = await _sessionRepository.GetByIdAsync(request.SessionId, cancellationToken)
            ?? throw new DomainException("Domain:SessaoNaoEncontrada");

        if (session.OwnerUserId != request.RequestingUserId)
            throw new DomainException("Domain:UsuarioNaoAutorizado");

        return session.Proposals
            .OrderByDescending(p => p.CreatedAt)
            .Select(p => new NegotiationProposalDto
            {
                ProposalId = p.ProposalId,
                SessionId = p.SessionId,
                PartyId = p.PartyId,
                Source = p.Source,
                Status = p.Status,
                Content = p.Content,
                RejectionReason = p.RejectionReason,
                CreatedAt = p.CreatedAt,
                RespondedAt = p.RespondedAt
            })
            .ToList();
    }
}
