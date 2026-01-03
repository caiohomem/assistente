using System.Linq;
using AssistenteExecutivo.Application.DTOs;
using AssistenteExecutivo.Application.Interfaces;
using AssistenteExecutivo.Application.Queries.Negotiations;
using AssistenteExecutivo.Domain.Exceptions;
using AssistenteExecutivo.Domain.Interfaces;
using MediatR;

namespace AssistenteExecutivo.Application.Handlers.Negotiations;

public class ListNegotiationSessionsQueryHandler : IRequestHandler<ListNegotiationSessionsQuery, List<NegotiationSessionDto>>
{
    private readonly INegotiationSessionRepository _sessionRepository;
    private readonly IClock _clock;

    public ListNegotiationSessionsQueryHandler(
        INegotiationSessionRepository sessionRepository,
        IClock clock)
    {
        _sessionRepository = sessionRepository;
        _clock = clock;
    }

    public async Task<List<NegotiationSessionDto>> Handle(ListNegotiationSessionsQuery request, CancellationToken cancellationToken)
    {
        if (request.OwnerUserId == Guid.Empty)
            throw new DomainException("Domain:OwnerUserIdObrigatorio");

        var sessions = await _sessionRepository.ListByOwnerAsync(request.OwnerUserId, cancellationToken);

        if (request.Status.HasValue)
        {
            sessions = sessions
                .Where(s => s.Status == request.Status.Value)
                .ToList();
        }

        return sessions
            .OrderByDescending(s => s.UpdatedAt)
            .Select(s => NegotiationSessionMapper.Map(s, _clock))
            .ToList();
    }
}
