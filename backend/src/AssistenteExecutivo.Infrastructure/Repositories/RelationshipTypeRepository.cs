using AssistenteExecutivo.Application.Interfaces;
using AssistenteExecutivo.Domain.Entities;
using AssistenteExecutivo.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace AssistenteExecutivo.Infrastructure.Repositories;

public class RelationshipTypeRepository : IRelationshipTypeRepository
{
    private readonly ApplicationDbContext _context;

    public RelationshipTypeRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<RelationshipType?> GetByIdAsync(Guid relationshipTypeId, Guid ownerUserId, CancellationToken cancellationToken = default)
    {
        return await _context.RelationshipTypes
            .AsNoTracking()
            .FirstOrDefaultAsync(rt => rt.RelationshipTypeId == relationshipTypeId && rt.OwnerUserId == ownerUserId, cancellationToken);
    }

    public async Task<List<RelationshipType>> GetByOwnerAsync(Guid ownerUserId, CancellationToken cancellationToken = default)
    {
        return await _context.RelationshipTypes
            .Where(rt => rt.OwnerUserId == ownerUserId)
            .OrderBy(rt => rt.CreatedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task<bool> ExistsWithNameAsync(Guid ownerUserId, string name, Guid? excludeRelationshipTypeId = null, CancellationToken cancellationToken = default)
    {
        var trimmedName = name.Trim();
        var query = _context.RelationshipTypes
            .Where(rt => rt.OwnerUserId == ownerUserId && rt.Name.ToLower() == trimmedName.ToLower());

        if (excludeRelationshipTypeId.HasValue)
        {
            query = query.Where(rt => rt.RelationshipTypeId != excludeRelationshipTypeId.Value);
        }

        return await query.AnyAsync(cancellationToken);
    }

    public async Task AddAsync(RelationshipType relationshipType, CancellationToken cancellationToken = default)
    {
        await _context.RelationshipTypes.AddAsync(relationshipType, cancellationToken);
    }

    public async Task AddRangeAsync(IEnumerable<RelationshipType> relationshipTypes, CancellationToken cancellationToken = default)
    {
        await _context.RelationshipTypes.AddRangeAsync(relationshipTypes, cancellationToken);
    }

    public Task UpdateAsync(RelationshipType relationshipType, CancellationToken cancellationToken = default)
    {
        _context.RelationshipTypes.Update(relationshipType);
        return Task.CompletedTask;
    }

    public Task DeleteAsync(RelationshipType relationshipType, CancellationToken cancellationToken = default)
    {
        _context.RelationshipTypes.Remove(relationshipType);
        return Task.CompletedTask;
    }
}
