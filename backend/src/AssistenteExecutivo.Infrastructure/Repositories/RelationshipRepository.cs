using AssistenteExecutivo.Application.Interfaces;
using AssistenteExecutivo.Domain.Entities;
using AssistenteExecutivo.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace AssistenteExecutivo.Infrastructure.Repositories;

public class RelationshipRepository : IRelationshipRepository
{
    private readonly ApplicationDbContext _context;

    public RelationshipRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<Relationship?> GetByIdAsync(Guid relationshipId, CancellationToken cancellationToken = default)
    {
        return await _context.Relationships
            .FirstOrDefaultAsync(r => r.RelationshipId == relationshipId, cancellationToken);
    }

    public async Task<List<Relationship>> GetByContactIdAsync(Guid contactId, Guid ownerUserId, CancellationToken cancellationToken = default)
    {
        // Primeiro verifica se o contato pertence ao ownerUserId
        var contactExists = await _context.Contacts
            .AnyAsync(c => c.ContactId == contactId && c.OwnerUserId == ownerUserId && !c.IsDeleted, cancellationToken);

        if (!contactExists)
            return new List<Relationship>();

        // Busca relacionamentos onde o contactId é SourceContactId ou TargetContactId
        return await _context.Relationships
            .Where(r => r.SourceContactId == contactId || r.TargetContactId == contactId)
            .ToListAsync(cancellationToken);
    }

    public async Task<Relationship?> GetBySourceAndTargetAsync(Guid sourceContactId, Guid targetContactId, CancellationToken cancellationToken = default)
    {
        return await _context.Relationships
            .FirstOrDefaultAsync(
                r => r.SourceContactId == sourceContactId && r.TargetContactId == targetContactId,
                cancellationToken);
    }

    public async Task AddAsync(Relationship relationship, CancellationToken cancellationToken = default)
    {
        await _context.Relationships.AddAsync(relationship, cancellationToken);
    }

    public async Task UpdateAsync(Relationship relationship, CancellationToken cancellationToken = default)
    {
        _context.Relationships.Update(relationship);
        await Task.CompletedTask;
    }

    public async Task DeleteAsync(Relationship relationship, CancellationToken cancellationToken = default)
    {
        _context.Relationships.Remove(relationship);
        await Task.CompletedTask;
    }

    public async Task UpdateRelationshipTypeNameAsync(Guid relationshipTypeId, string typeName, CancellationToken cancellationToken = default)
    {
        await _context.Relationships
            .Where(r => r.RelationshipTypeId == relationshipTypeId)
            .ExecuteUpdateAsync(setters => setters
                .SetProperty(r => r.Type, typeName.Trim()),
                cancellationToken);
    }

    public async Task RemoveRelationshipTypeReferenceAsync(Guid relationshipTypeId, CancellationToken cancellationToken = default)
    {
        await _context.Relationships
            .Where(r => r.RelationshipTypeId == relationshipTypeId)
            .ExecuteUpdateAsync(setters => setters
                .SetProperty(r => r.RelationshipTypeId, (Guid?)null),
                cancellationToken);
    }
}
