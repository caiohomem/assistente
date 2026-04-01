using AssistenteExecutivo.Application.Interfaces;
using AssistenteExecutivo.Domain.Entities;
using AssistenteExecutivo.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace AssistenteExecutivo.Infrastructure.Repositories;

public class CommissionAgreementRepository : ICommissionAgreementRepository
{
    private readonly ApplicationDbContext _context;

    public CommissionAgreementRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<CommissionAgreement?> GetByIdAsync(Guid agreementId, CancellationToken cancellationToken = default)
    {
        return await _context.CommissionAgreements
            .Include(a => a.Parties)
            .Include(a => a.Milestones)
            .FirstOrDefaultAsync(a => a.AgreementId == agreementId, cancellationToken);
    }

    public async Task<CommissionAgreement?> GetByPartyIdAsync(Guid partyId, CancellationToken cancellationToken = default)
    {
        return await _context.CommissionAgreements
            .Include(a => a.Parties)
            .Include(a => a.Milestones)
            .FirstOrDefaultAsync(a => a.Parties.Any(p => p.PartyId == partyId), cancellationToken);
    }

    public async Task<List<CommissionAgreement>> ListByOwnerAsync(Guid ownerUserId, CancellationToken cancellationToken = default)
    {
        return await _context.CommissionAgreements
            .Where(a => a.OwnerUserId == ownerUserId)
            .Include(a => a.Parties)
            .Include(a => a.Milestones)
            .OrderByDescending(a => a.UpdatedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task AddAsync(CommissionAgreement agreement, CancellationToken cancellationToken = default)
    {
        await _context.CommissionAgreements.AddAsync(agreement, cancellationToken);
    }

    public Task UpdateAsync(CommissionAgreement agreement, CancellationToken cancellationToken = default)
    {
        var entry = _context.Entry(agreement);
        if (entry.State == EntityState.Detached)
        {
            _context.CommissionAgreements.Update(agreement);
        }
        return Task.CompletedTask;
    }
}
