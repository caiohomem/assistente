using AssistenteExecutivo.Domain.Exceptions;
using AssistenteExecutivo.Domain.Interfaces;

namespace AssistenteExecutivo.Domain.Entities;

public class RelationshipType
{
    private RelationshipType() { } // EF Core

    public RelationshipType(
        Guid relationshipTypeId,
        Guid ownerUserId,
        string name,
        IClock clock,
        bool isDefault = false)
    {
        if (relationshipTypeId == Guid.Empty)
            throw new DomainException("Domain:RelationshipTypeIdObrigatorio");

        if (ownerUserId == Guid.Empty)
            throw new DomainException("Domain:OwnerUserIdObrigatorio");

        if (string.IsNullOrWhiteSpace(name))
            throw new DomainException("Domain:RelationshipTypeNomeObrigatorio");

        RelationshipTypeId = relationshipTypeId;
        OwnerUserId = ownerUserId;
        Name = name.Trim();
        IsDefault = isDefault;
        CreatedAt = clock.UtcNow;
        UpdatedAt = clock.UtcNow;
    }

    public Guid RelationshipTypeId { get; private set; }
    public Guid OwnerUserId { get; private set; }
    public string Name { get; private set; } = null!;
    public bool IsDefault { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime UpdatedAt { get; private set; }

    public void UpdateName(string name, IClock clock)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new DomainException("Domain:RelationshipTypeNomeObrigatorio");

        Name = name.Trim();
        UpdatedAt = clock.UtcNow;
    }

    public void MarkAsCustom(IClock clock)
    {
        if (IsDefault)
        {
            IsDefault = false;
            UpdatedAt = clock.UtcNow;
        }
    }
}
