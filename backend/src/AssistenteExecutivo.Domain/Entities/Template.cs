using AssistenteExecutivo.Domain.DomainEvents;
using AssistenteExecutivo.Domain.Enums;
using AssistenteExecutivo.Domain.Exceptions;
using AssistenteExecutivo.Domain.Interfaces;

namespace AssistenteExecutivo.Domain.Entities;

public class Template
{
    private readonly List<IDomainEvent> _domainEvents = new();

    private Template() { } // EF Core

    public Template(
        Guid templateId,
        Guid ownerUserId,
        string name,
        TemplateType type,
        string body,
        IClock clock)
    {
        if (templateId == Guid.Empty)
            throw new DomainException("Domain:TemplateIdObrigatorio");

        if (ownerUserId == Guid.Empty)
            throw new DomainException("Domain:OwnerUserIdObrigatorio");

        if (string.IsNullOrWhiteSpace(name))
            throw new DomainException("Domain:TemplateNameObrigatorio");

        if (string.IsNullOrWhiteSpace(body))
            throw new DomainException("Domain:TemplateBodyObrigatorio");

        if (clock == null)
            throw new DomainException("Domain:ClockObrigatorio");

        TemplateId = templateId;
        OwnerUserId = ownerUserId;
        Name = name.Trim();
        Type = type;
        Body = body;
        PlaceholdersSchema = null; // JSON schema para placeholders
        Active = true;
        CreatedAt = clock.UtcNow;
        UpdatedAt = clock.UtcNow;
    }

    public Guid TemplateId { get; private set; }
    public Guid OwnerUserId { get; private set; }
    public string Name { get; private set; } = null!;
    public TemplateType Type { get; private set; }
    public string Body { get; private set; } = null!;
    public string? PlaceholdersSchema { get; private set; } // JSON schema
    public bool Active { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime UpdatedAt { get; private set; }

    public IReadOnlyCollection<IDomainEvent> DomainEvents => _domainEvents.AsReadOnly();

    public static Template Create(
        Guid templateId,
        Guid ownerUserId,
        string name,
        TemplateType type,
        string body,
        IClock clock)
    {
        var template = new Template(templateId, ownerUserId, name, type, body, clock);
        template._domainEvents.Add(new TemplateCreated(
            template.TemplateId,
            template.OwnerUserId,
            template.Name,
            template.Type,
            clock.UtcNow));
        return template;
    }

    public void UpdateName(string name, IClock clock)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new DomainException("Domain:TemplateNameObrigatorio");

        Name = name.Trim();
        UpdatedAt = clock.UtcNow;
    }

    public void UpdateBody(string body, IClock clock)
    {
        if (string.IsNullOrWhiteSpace(body))
            throw new DomainException("Domain:TemplateBodyObrigatorio");

        Body = body;
        UpdatedAt = clock.UtcNow;
    }

    public void UpdatePlaceholdersSchema(string? placeholdersSchema, IClock clock)
    {
        PlaceholdersSchema = placeholdersSchema;
        UpdatedAt = clock.UtcNow;
    }

    public void Activate(IClock clock)
    {
        if (Active)
            return;

        Active = true;
        UpdatedAt = clock.UtcNow;
    }

    public void Deactivate(IClock clock)
    {
        if (!Active)
            return;

        Active = false;
        UpdatedAt = clock.UtcNow;
    }

    public void ClearDomainEvents()
    {
        _domainEvents.Clear();
    }
}





