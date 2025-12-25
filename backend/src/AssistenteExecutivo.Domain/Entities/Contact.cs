using AssistenteExecutivo.Domain.DomainEvents;
using AssistenteExecutivo.Domain.Exceptions;
using AssistenteExecutivo.Domain.Interfaces;
using AssistenteExecutivo.Domain.ValueObjects;

namespace AssistenteExecutivo.Domain.Entities;

public class Contact
{
    private readonly List<EmailAddress> _emails = new();
    private readonly List<PhoneNumber> _phones = new();
    private readonly List<Tag> _tags = new();
    private readonly List<Relationship> _relationships = new();
    private readonly List<IDomainEvent> _domainEvents = new();

    private Contact() { } // EF Core

    public Contact(
        Guid contactId,
        Guid ownerUserId,
        PersonName name,
        IClock clock)
    {
        if (contactId == Guid.Empty)
            throw new DomainException("Domain:ContactIdObrigatorio");

        if (ownerUserId == Guid.Empty)
            throw new DomainException("Domain:OwnerUserIdObrigatorio");

        if (name == null)
            throw new DomainException("Domain:NomeObrigatorio");

        if (clock == null)
            throw new DomainException("Domain:ClockObrigatorio");

        ContactId = contactId;
        OwnerUserId = ownerUserId;
        Name = name;
        CreatedAt = clock.UtcNow;
        UpdatedAt = clock.UtcNow;
        IsDeleted = false;
    }

    public Guid ContactId { get; private set; }
    public Guid OwnerUserId { get; private set; }
    public PersonName Name { get; private set; } = null!;
    public string? JobTitle { get; private set; }
    public string? Company { get; private set; }
    public IReadOnlyCollection<EmailAddress> Emails => _emails.AsReadOnly();
    public IReadOnlyCollection<PhoneNumber> Phones => _phones.AsReadOnly();
    public Address? Address { get; private set; }
    public IReadOnlyCollection<Tag> Tags => _tags.AsReadOnly();
    public IReadOnlyCollection<Relationship> Relationships => _relationships.AsReadOnly();
    public DateTime CreatedAt { get; private set; }
    public DateTime UpdatedAt { get; private set; }
    public bool IsDeleted { get; private set; }

    public IReadOnlyCollection<IDomainEvent> DomainEvents => _domainEvents.AsReadOnly();

    public static Contact Create(
        Guid contactId,
        Guid ownerUserId,
        PersonName name,
        IClock clock)
    {
        var contact = new Contact(contactId, ownerUserId, name, clock);
        contact._domainEvents.Add(new ContactCreated(contactId, ownerUserId, "Manual", clock.UtcNow));
        return contact;
    }

    public static Contact CreateFromCardExtract(
        Guid contactId,
        Guid ownerUserId,
        OcrExtract extract,
        IClock clock)
    {
        if (extract == null)
            throw new DomainException("Domain:OcrExtractObrigatorio");

        if (!extract.HasMinimumData)
            throw new DomainException("Domain:ContatoDeveTerEmailOuTelefone");

        var name = !string.IsNullOrWhiteSpace(extract.Name)
            ? PersonName.Create(extract.Name)
            : PersonName.Create("Contato sem nome");

        var contact = new Contact(contactId, ownerUserId, name, clock);

        if (!string.IsNullOrWhiteSpace(extract.Email))
        {
            contact.AddEmail(EmailAddress.Create(extract.Email));
        }

        if (!string.IsNullOrWhiteSpace(extract.Phone))
        {
            contact.AddPhone(PhoneNumber.Create(extract.Phone));
        }

        contact._domainEvents.Add(new ContactCreated(contactId, ownerUserId, "OCR", clock.UtcNow));

        return contact;
    }

    public void AddEmail(EmailAddress email)
    {
        if (email == null)
            throw new DomainException("Domain:EmailObrigatorio");

        if (_emails.Any(e => e == email))
            throw new DomainException("Domain:EmailJaExiste");

        _emails.Add(email);
        UpdatedAt = DateTime.UtcNow;
    }

    public void AddPhone(PhoneNumber phone)
    {
        if (phone == null)
            throw new DomainException("Domain:TelefoneObrigatorio");

        if (_phones.Any(p => p == phone))
            throw new DomainException("Domain:TelefoneJaExiste");

        _phones.Add(phone);
        UpdatedAt = DateTime.UtcNow;
    }

    public void UpdateDetails(
        PersonName? name = null,
        string? jobTitle = null,
        string? company = null,
        Address? address = null)
    {
        if (IsDeleted)
            throw new DomainException("Domain:ContatoDeletadoNaoPodeSerAtualizado");

        if (name != null)
        {
            Name = name;
        }

        if (jobTitle != null)
        {
            JobTitle = jobTitle.Trim();
        }

        if (company != null)
        {
            Company = company.Trim();
        }

        if (address != null)
        {
            Address = address;
        }

        UpdatedAt = DateTime.UtcNow;
        _domainEvents.Add(new ContactUpdated(ContactId, DateTime.UtcNow));
    }

    public Relationship AddRelationship(
        Guid relationshipId,
        Guid targetContactId,
        string type,
        string? description,
        IClock clock)
    {
        if (IsDeleted)
            throw new DomainException("Domain:ContatoDeletadoNaoPodeTerRelacionamentos");

        if (_relationships.Any(r => r.TargetContactId == targetContactId && r.SourceContactId == ContactId))
            throw new DomainException("Domain:RelationshipJaExiste");

        var relationship = new Relationship(relationshipId, ContactId, targetContactId, type, description);
        _relationships.Add(relationship);
        UpdatedAt = clock.UtcNow;
        return relationship;
    }

    public void ConfirmRelationship(Guid relationshipId)
    {
        var relationship = _relationships.FirstOrDefault(r => r.RelationshipId == relationshipId);
        if (relationship == null)
            throw new DomainException("Domain:RelationshipNaoEncontrado");

        relationship.Confirm();
        UpdatedAt = DateTime.UtcNow;
    }

    public void UpdateRelationshipStrength(Guid relationshipId, float strength)
    {
        var relationship = _relationships.FirstOrDefault(r => r.RelationshipId == relationshipId);
        if (relationship == null)
            throw new DomainException("Domain:RelationshipNaoEncontrado");

        relationship.UpdateStrength(strength);
        UpdatedAt = DateTime.UtcNow;
    }

    public void AddTag(Tag tag)
    {
        if (tag == null)
            throw new DomainException("Domain:TagObrigatoria");

        if (_tags.Any(t => t == tag))
            return; // Tag já existe, não adiciona duplicada

        _tags.Add(tag);
        UpdatedAt = DateTime.UtcNow;
    }

    public void Delete()
    {
        if (IsDeleted)
            return;

        // Regra de negócio: Contact só pode ser deletado se não tiver Relationships ativos
        if (_relationships.Any(r => r.IsConfirmed))
            throw new DomainException("Domain:ContactComRelacionamentosConfirmadosNaoPodeSerDeletado");

        IsDeleted = true;
        UpdatedAt = DateTime.UtcNow;

        _domainEvents.Add(new ContactDeleted(ContactId, DateTime.UtcNow));
    }

    public void ClearDomainEvents()
    {
        _domainEvents.Clear();
    }

    public void EnsureHasEmailOrPhone()
    {
        if (_emails.Count == 0 && _phones.Count == 0)
            throw new DomainException("Domain:ContatoDeveTerEmailOuTelefone");
    }
}
