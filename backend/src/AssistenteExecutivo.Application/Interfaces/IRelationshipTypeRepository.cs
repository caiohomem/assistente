using AssistenteExecutivo.Domain.Entities;

namespace AssistenteExecutivo.Application.Interfaces;

public interface IRelationshipTypeRepository
{
    Task<RelationshipType?> GetByIdAsync(Guid relationshipTypeId, Guid ownerUserId, CancellationToken cancellationToken = default);
    Task<List<RelationshipType>> GetByOwnerAsync(Guid ownerUserId, CancellationToken cancellationToken = default);
    Task<bool> ExistsWithNameAsync(Guid ownerUserId, string name, Guid? excludeRelationshipTypeId = null, CancellationToken cancellationToken = default);
    Task AddAsync(RelationshipType relationshipType, CancellationToken cancellationToken = default);
    Task AddRangeAsync(IEnumerable<RelationshipType> relationshipTypes, CancellationToken cancellationToken = default);
    Task UpdateAsync(RelationshipType relationshipType, CancellationToken cancellationToken = default);
    Task DeleteAsync(RelationshipType relationshipType, CancellationToken cancellationToken = default);
}
