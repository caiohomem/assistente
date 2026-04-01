using AssistenteExecutivo.Domain.Entities;

namespace AssistenteExecutivo.Application.Interfaces;

public interface IRelationshipRepository
{
    Task<Relationship?> GetByIdAsync(Guid relationshipId, CancellationToken cancellationToken = default);
    Task<List<Relationship>> GetByContactIdAsync(Guid contactId, Guid ownerUserId, CancellationToken cancellationToken = default);
    Task<Relationship?> GetBySourceAndTargetAsync(Guid sourceContactId, Guid targetContactId, CancellationToken cancellationToken = default);
    Task AddAsync(Relationship relationship, CancellationToken cancellationToken = default);
    Task UpdateAsync(Relationship relationship, CancellationToken cancellationToken = default);
    Task DeleteAsync(Relationship relationship, CancellationToken cancellationToken = default);
    Task UpdateRelationshipTypeNameAsync(Guid relationshipTypeId, string typeName, CancellationToken cancellationToken = default);
    Task RemoveRelationshipTypeReferenceAsync(Guid relationshipTypeId, CancellationToken cancellationToken = default);
}













