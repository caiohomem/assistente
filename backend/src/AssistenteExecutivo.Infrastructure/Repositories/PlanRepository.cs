using AssistenteExecutivo.Application.Interfaces;
using AssistenteExecutivo.Domain.Entities;
using AssistenteExecutivo.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace AssistenteExecutivo.Infrastructure.Repositories;

public class PlanRepository : IPlanRepository
{
    private readonly ApplicationDbContext _context;

    public PlanRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<Plan?> GetByIdAsync(Guid planId, CancellationToken cancellationToken = default)
    {
        return await _context.Plans
            .FirstOrDefaultAsync(p => p.PlanId == planId, cancellationToken);
    }

    public async Task<List<Plan>> GetAllAsync(bool includeInactive = false, CancellationToken cancellationToken = default)
    {
        var query = _context.Plans.AsQueryable();

        if (!includeInactive)
        {
            query = query.Where(p => p.IsActive);
        }

        return await query
            .OrderBy(p => p.Price)
            .ToListAsync(cancellationToken);
    }

    public async Task<List<Plan>> GetActiveAsync(CancellationToken cancellationToken = default)
    {
        return await _context.Plans
            .Where(p => p.IsActive)
            .OrderBy(p => p.Price)
            .ToListAsync(cancellationToken);
    }

    public async Task AddAsync(Plan plan, CancellationToken cancellationToken = default)
    {
        await _context.Plans.AddAsync(plan, cancellationToken);
    }

    public async Task UpdateAsync(Plan plan, CancellationToken cancellationToken = default)
    {
        _context.Plans.Update(plan);
        await Task.CompletedTask;
    }

    public async Task<bool> ExistsAsync(Guid planId, CancellationToken cancellationToken = default)
    {
        return await _context.Plans
            .AnyAsync(p => p.PlanId == planId, cancellationToken);
    }
}






