using AssistenteExecutivo.Application.DTOs;
using AssistenteExecutivo.Application.Interfaces;
using AssistenteExecutivo.Application.Queries.Negotiations;
using AssistenteExecutivo.Domain.Exceptions;
using AssistenteExecutivo.Domain.Interfaces;
using MediatR;

namespace AssistenteExecutivo.Application.Handlers.Negotiations;

public class GetNegotiationSessionByIdQueryHandler : IRequestHandler<GetNegotiationSessionByIdQuery, NegotiationSessionDto?>
{
    private readonly INegotiationSessionRepository _sessionRepository;
    private readonly IClock _clock;

    public GetNegotiationSessionByIdQueryHandler(
        INegotiationSessionRepository sessionRepository,
        IClock clock)
    {
        _sessionRepository = sessionRepository;
        _clock = clock;
    }

    public async Task<NegotiationSessionDto?> Handle(GetNegotiationSessionByIdQuery request, CancellationToken cancellationToken)
    {
        if (request.RequestingUserId == Guid.Empty)
            throw new DomainException("Domain:UsuarioSolicitanteObrigatorio");

        var session = await _sessionRepository.GetByIdAsync(request.SessionId, cancellationToken);
        if (session == null)
            return null;

        EnsureOwner(session, request.RequestingUserId);
        return NegotiationSessionMapper.Map(session, _clock);
    }

    private static void EnsureOwner(Domain.Entities.NegotiationSession session, Guid userId)
    {
        if (session.OwnerUserId != userId)
            throw new DomainException("Domain:UsuarioNaoAutorizado");
    }
}
