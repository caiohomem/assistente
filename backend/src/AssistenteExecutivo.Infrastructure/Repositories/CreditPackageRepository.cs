using AssistenteExecutivo.Application.Interfaces;
using AssistenteExecutivo.Domain.Entities;
using AssistenteExecutivo.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace AssistenteExecutivo.Infrastructure.Repositories;

public class CreditPackageRepository : ICreditPackageRepository
{
    private readonly ApplicationDbContext _context;

    public CreditPackageRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<CreditPackage?> GetByIdAsync(Guid packageId, CancellationToken cancellationToken = default)
    {
        return await _context.CreditPackages
            .FirstOrDefaultAsync(p => p.PackageId == packageId, cancellationToken);
    }

    public async Task<List<CreditPackage>> GetAllAsync(bool includeInactive = false, CancellationToken cancellationToken = default)
    {
        var query = _context.CreditPackages.AsQueryable();

        if (!includeInactive)
        {
            query = query.Where(p => p.IsActive);
        }

        return await query
            .OrderBy(p => p.Price)
            .ToListAsync(cancellationToken);
    }

    public async Task<List<CreditPackage>> GetActiveAsync(CancellationToken cancellationToken = default)
    {
        return await _context.CreditPackages
            .Where(p => p.IsActive)
            .OrderBy(p => p.Price)
            .ToListAsync(cancellationToken);
    }

    public async Task AddAsync(CreditPackage package, CancellationToken cancellationToken = default)
    {
        await _context.CreditPackages.AddAsync(package, cancellationToken);
    }

    public async Task UpdateAsync(CreditPackage package, CancellationToken cancellationToken = default)
    {
        var entry = _context.Entry(package);
        
        if (entry.State == EntityState.Detached)
        {
            _context.CreditPackages.Update(package);
        }
        
        await Task.CompletedTask;
    }

    public async Task<bool> ExistsAsync(Guid packageId, CancellationToken cancellationToken = default)
    {
        return await _context.CreditPackages
            .AnyAsync(p => p.PackageId == packageId, cancellationToken);
    }
}

