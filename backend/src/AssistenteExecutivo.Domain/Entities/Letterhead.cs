using AssistenteExecutivo.Domain.DomainEvents;
using AssistenteExecutivo.Domain.Exceptions;
using AssistenteExecutivo.Domain.Interfaces;

namespace AssistenteExecutivo.Domain.Entities;

public class Letterhead
{
    private readonly List<IDomainEvent> _domainEvents = new();

    private Letterhead() { } // EF Core

    public Letterhead(
        Guid letterheadId,
        Guid ownerUserId,
        string name,
        string designData,
        IClock clock)
    {
        if (letterheadId == Guid.Empty)
            throw new DomainException("Domain:LetterheadIdObrigatorio");

        if (ownerUserId == Guid.Empty)
            throw new DomainException("Domain:OwnerUserIdObrigatorio");

        if (string.IsNullOrWhiteSpace(name))
            throw new DomainException("Domain:LetterheadNameObrigatorio");

        if (string.IsNullOrWhiteSpace(designData))
            throw new DomainException("Domain:LetterheadDesignDataObrigatorio");

        if (clock == null)
            throw new DomainException("Domain:ClockObrigatorio");

        LetterheadId = letterheadId;
        OwnerUserId = ownerUserId;
        Name = name.Trim();
        DesignData = designData; // JSON com dados do design
        IsActive = true;
        CreatedAt = clock.UtcNow;
        UpdatedAt = clock.UtcNow;
    }

    public Guid LetterheadId { get; private set; }
    public Guid OwnerUserId { get; private set; }
    public string Name { get; private set; } = null!;
    public string DesignData { get; private set; } = null!; // JSON
    public bool IsActive { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime UpdatedAt { get; private set; }

    public IReadOnlyCollection<IDomainEvent> DomainEvents => _domainEvents.AsReadOnly();

    public static Letterhead Create(
        Guid letterheadId,
        Guid ownerUserId,
        string name,
        string designData,
        IClock clock)
    {
        var letterhead = new Letterhead(letterheadId, ownerUserId, name, designData, clock);
        letterhead._domainEvents.Add(new LetterheadCreated(
            letterhead.LetterheadId,
            letterhead.OwnerUserId,
            letterhead.Name,
            clock.UtcNow));
        return letterhead;
    }

    public void UpdateName(string name, IClock clock)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new DomainException("Domain:LetterheadNameObrigatorio");

        Name = name.Trim();
        UpdatedAt = clock.UtcNow;
    }

    public void UpdateDesignData(string designData, IClock clock)
    {
        if (string.IsNullOrWhiteSpace(designData))
            throw new DomainException("Domain:LetterheadDesignDataObrigatorio");

        DesignData = designData;
        UpdatedAt = clock.UtcNow;
    }

    public void Activate(IClock clock)
    {
        if (IsActive)
            return;

        IsActive = true;
        UpdatedAt = clock.UtcNow;
    }

    public void Deactivate(IClock clock)
    {
        if (!IsActive)
            return;

        IsActive = false;
        UpdatedAt = clock.UtcNow;
    }

    public void ClearDomainEvents()
    {
        _domainEvents.Clear();
    }
}









