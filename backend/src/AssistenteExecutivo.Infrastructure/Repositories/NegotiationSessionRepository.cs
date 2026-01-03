using AssistenteExecutivo.Application.Interfaces;
using AssistenteExecutivo.Domain.Entities;
using AssistenteExecutivo.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace AssistenteExecutivo.Infrastructure.Repositories;

public class NegotiationSessionRepository : INegotiationSessionRepository
{
    private readonly ApplicationDbContext _context;

    public NegotiationSessionRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<NegotiationSession?> GetByIdAsync(Guid sessionId, CancellationToken cancellationToken = default)
    {
        return await _context.NegotiationSessions
            .Include(s => s.Proposals)
            .FirstOrDefaultAsync(s => s.SessionId == sessionId, cancellationToken);
    }

    public async Task<List<NegotiationSession>> ListByOwnerAsync(Guid ownerUserId, CancellationToken cancellationToken = default)
    {
        return await _context.NegotiationSessions
            .Where(s => s.OwnerUserId == ownerUserId)
            .Include(s => s.Proposals)
            .OrderByDescending(s => s.UpdatedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task<List<NegotiationSession>> ListByAgreementAsync(Guid agreementId, CancellationToken cancellationToken = default)
    {
        return await _context.NegotiationSessions
            .Where(s => s.GeneratedAgreementId == agreementId)
            .Include(s => s.Proposals)
            .ToListAsync(cancellationToken);
    }

    public async Task AddAsync(NegotiationSession session, CancellationToken cancellationToken = default)
    {
        await _context.NegotiationSessions.AddAsync(session, cancellationToken);
    }

    public Task UpdateAsync(NegotiationSession session, CancellationToken cancellationToken = default)
    {
        _context.NegotiationSessions.Update(session);
        return Task.CompletedTask;
    }
}
