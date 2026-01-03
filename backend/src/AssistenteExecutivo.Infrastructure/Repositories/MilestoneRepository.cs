using AssistenteExecutivo.Application.Interfaces;
using AssistenteExecutivo.Domain.Entities;
using AssistenteExecutivo.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace AssistenteExecutivo.Infrastructure.Repositories;

public class MilestoneRepository : IMilestoneRepository
{
    private readonly ApplicationDbContext _context;

    public MilestoneRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<Milestone?> GetByIdAsync(Guid milestoneId, CancellationToken cancellationToken = default)
    {
        return await _context.Milestones
            .FirstOrDefaultAsync(m => m.MilestoneId == milestoneId, cancellationToken);
    }

    public async Task<List<Milestone>> ListByAgreementAsync(Guid agreementId, CancellationToken cancellationToken = default)
    {
        return await _context.Milestones
            .Where(m => m.AgreementId == agreementId)
            .OrderBy(m => m.DueDate)
            .ToListAsync(cancellationToken);
    }

    public async Task AddAsync(Milestone milestone, CancellationToken cancellationToken = default)
    {
        await _context.Milestones.AddAsync(milestone, cancellationToken);
    }

    public Task UpdateAsync(Milestone milestone, CancellationToken cancellationToken = default)
    {
        _context.Milestones.Update(milestone);
        return Task.CompletedTask;
    }
}
