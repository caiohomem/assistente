using AssistenteExecutivo.Domain.Exceptions;
using AssistenteExecutivo.Domain.Interfaces;

namespace AssistenteExecutivo.Domain.Entities;

public class Relationship
{
    private Relationship() { } // EF Core

    public Relationship(
        Guid relationshipId,
        Guid sourceContactId,
        Guid targetContactId,
        string type,
        string? description = null)
    {
        if (relationshipId == Guid.Empty)
            throw new DomainException("Domain:RelationshipIdObrigatorio");

        if (sourceContactId == Guid.Empty)
            throw new DomainException("Domain:SourceContactIdObrigatorio");

        if (targetContactId == Guid.Empty)
            throw new DomainException("Domain:TargetContactIdObrigatorio");

        if (sourceContactId == targetContactId)
            throw new DomainException("Domain:RelationshipNaoPodeSerComMesmoContato");

        if (string.IsNullOrWhiteSpace(type))
            throw new DomainException("Domain:RelationshipTypeObrigatorio");

        RelationshipId = relationshipId;
        SourceContactId = sourceContactId;
        TargetContactId = targetContactId;
        Type = type.Trim();
        Description = description?.Trim();
        Strength = 0.0f; // Calculado posteriormente
        IsConfirmed = false;
    }

    public Guid RelationshipId { get; private set; }
    public Guid SourceContactId { get; private set; }
    public Guid TargetContactId { get; private set; }
    public string Type { get; private set; } = null!;
    public string? Description { get; private set; }
    public float Strength { get; private set; }
    public bool IsConfirmed { get; private set; }

    public void UpdateStrength(float strength)
    {
        if (strength < 0.0f || strength > 1.0f)
            throw new DomainException("Domain:StrengthDeveEstarEntreZeroEUm");

        Strength = strength;
    }

    public void Confirm()
    {
        IsConfirmed = true;
    }

    public void UpdateDescription(string? description)
    {
        Description = description?.Trim();
    }
}


