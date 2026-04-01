using System.Collections.Concurrent;
using AssistenteExecutivo.Application.Interfaces;
using AssistenteExecutivo.Domain.Entities;

namespace AssistenteExecutivo.Infrastructure.Repositories;

public class InMemoryNegotiationSessionRepository : INegotiationSessionRepository
{
    private static readonly ConcurrentDictionary<Guid, NegotiationSession> Sessions = new();

    public Task<NegotiationSession?> GetByIdAsync(Guid sessionId, CancellationToken cancellationToken = default)
    {
        Sessions.TryGetValue(sessionId, out var session);
        return Task.FromResult(session);
    }

    public Task<List<NegotiationSession>> ListByOwnerAsync(Guid ownerUserId, CancellationToken cancellationToken = default)
    {
        var list = Sessions.Values.Where(s => s.OwnerUserId == ownerUserId).ToList();
        return Task.FromResult(list);
    }

    public Task<List<NegotiationSession>> ListByAgreementAsync(Guid agreementId, CancellationToken cancellationToken = default)
    {
        var list = Sessions.Values.Where(s => s.GeneratedAgreementId == agreementId).ToList();
        return Task.FromResult(list);
    }

    public Task AddAsync(NegotiationSession session, CancellationToken cancellationToken = default)
    {
        Sessions[session.SessionId] = session;
        return Task.CompletedTask;
    }

    public Task UpdateAsync(NegotiationSession session, CancellationToken cancellationToken = default)
    {
        Sessions[session.SessionId] = session;
        return Task.CompletedTask;
    }
}
