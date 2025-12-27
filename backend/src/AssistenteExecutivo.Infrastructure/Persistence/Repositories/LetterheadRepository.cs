using AssistenteExecutivo.Application.Interfaces;
using AssistenteExecutivo.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace AssistenteExecutivo.Infrastructure.Persistence.Repositories;

public class LetterheadRepository : ILetterheadRepository
{
    private readonly ApplicationDbContext _context;

    public LetterheadRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<Letterhead?> GetByIdAsync(Guid letterheadId, Guid ownerUserId, CancellationToken cancellationToken = default)
    {
        return await _context.Letterheads
            .FirstOrDefaultAsync(l => l.LetterheadId == letterheadId && l.OwnerUserId == ownerUserId, cancellationToken);
    }

    public async Task<List<Letterhead>> GetByOwnerIdAsync(Guid ownerUserId, CancellationToken cancellationToken = default)
    {
        return await _context.Letterheads
            .Where(l => l.OwnerUserId == ownerUserId)
            .ToListAsync(cancellationToken);
    }

    public async Task<List<Letterhead>> GetActiveByOwnerIdAsync(Guid ownerUserId, CancellationToken cancellationToken = default)
    {
        return await _context.Letterheads
            .Where(l => l.OwnerUserId == ownerUserId && l.IsActive)
            .ToListAsync(cancellationToken);
    }

    public async Task AddAsync(Letterhead letterhead, CancellationToken cancellationToken = default)
    {
        await _context.Letterheads.AddAsync(letterhead, cancellationToken);
    }

    public Task UpdateAsync(Letterhead letterhead, CancellationToken cancellationToken = default)
    {
        _context.Letterheads.Update(letterhead);
        return Task.CompletedTask;
    }

    public Task DeleteAsync(Letterhead letterhead, CancellationToken cancellationToken = default)
    {
        _context.Letterheads.Remove(letterhead);
        return Task.CompletedTask;
    }
}

