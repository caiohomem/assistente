using AssistenteExecutivo.Domain.DomainEvents;
using AssistenteExecutivo.Domain.Enums;
using AssistenteExecutivo.Domain.Exceptions;
using AssistenteExecutivo.Domain.Interfaces;

namespace AssistenteExecutivo.Domain.Entities;

public class DraftDocument
{
    private readonly List<IDomainEvent> _domainEvents = new();

    private DraftDocument() { } // EF Core

    public DraftDocument(
        Guid draftId,
        Guid ownerUserId,
        DocumentType documentType,
        string content,
        IClock clock)
    {
        if (draftId == Guid.Empty)
            throw new DomainException("Domain:DraftIdObrigatorio");

        if (ownerUserId == Guid.Empty)
            throw new DomainException("Domain:OwnerUserIdObrigatorio");

        if (string.IsNullOrWhiteSpace(content))
            throw new DomainException("Domain:DraftContentObrigatorio");

        if (clock == null)
            throw new DomainException("Domain:ClockObrigatorio");

        DraftId = draftId;
        OwnerUserId = ownerUserId;
        DocumentType = documentType;
        Content = content;
        Status = DraftStatus.Draft;
        CreatedAt = clock.UtcNow;
        UpdatedAt = clock.UtcNow;
    }

    public Guid DraftId { get; private set; }
    public Guid OwnerUserId { get; private set; }
    public Guid? ContactId { get; private set; }
    public Guid? CompanyId { get; private set; }
    public DocumentType DocumentType { get; private set; }
    public Guid? TemplateId { get; private set; }
    public Guid? LetterheadId { get; private set; }
    public string Content { get; private set; } = null!;
    public DraftStatus Status { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime UpdatedAt { get; private set; }

    public IReadOnlyCollection<IDomainEvent> DomainEvents => _domainEvents.AsReadOnly();

    public static DraftDocument Create(
        Guid draftId,
        Guid ownerUserId,
        DocumentType documentType,
        string content,
        IClock clock)
    {
        var draft = new DraftDocument(draftId, ownerUserId, documentType, content, clock);
        draft._domainEvents.Add(new DraftCreated(
            draft.DraftId,
            draft.OwnerUserId,
            draft.ContactId,
            draft.CompanyId,
            draft.DocumentType,
            clock.UtcNow));
        return draft;
    }

    public void AssociateContact(Guid contactId, IClock clock)
    {
        if (contactId == Guid.Empty)
            throw new DomainException("Domain:ContactIdObrigatorio");

        ContactId = contactId;
        UpdatedAt = clock.UtcNow;
    }

    public void AssociateCompany(Guid companyId, IClock clock)
    {
        if (companyId == Guid.Empty)
            throw new DomainException("Domain:CompanyIdObrigatorio");

        CompanyId = companyId;
        UpdatedAt = clock.UtcNow;
    }

    public void SetTemplate(Guid templateId, IClock clock)
    {
        if (templateId == Guid.Empty)
            throw new DomainException("Domain:TemplateIdObrigatorio");

        TemplateId = templateId;
        UpdatedAt = clock.UtcNow;
    }

    public void SetLetterhead(Guid letterheadId, IClock clock)
    {
        if (letterheadId == Guid.Empty)
            throw new DomainException("Domain:LetterheadIdObrigatorio");

        LetterheadId = letterheadId;
        UpdatedAt = clock.UtcNow;
    }

    public void UpdateContent(string content, IClock clock)
    {
        if (string.IsNullOrWhiteSpace(content))
            throw new DomainException("Domain:DraftContentObrigatorio");

        if (Status == DraftStatus.Sent)
            throw new DomainException("Domain:DraftEnviadoNaoPodeSerEditado");

        Content = content;
        UpdatedAt = clock.UtcNow;
    }

    public void Approve(Guid approvedBy, IClock clock)
    {
        if (Status != DraftStatus.Draft)
            throw new DomainException("Domain:DraftSoPodeSerAprovadoSeEmRascunho");

        Status = DraftStatus.Approved;
        UpdatedAt = clock.UtcNow;

        _domainEvents.Add(new DraftApproved(DraftId, approvedBy, clock.UtcNow));
    }

    public void Send(Guid sentBy, IClock clock)
    {
        if (Status != DraftStatus.Approved && Status != DraftStatus.Draft)
            throw new DomainException("Domain:DraftSoPodeSerEnviadoSeAprovadoOuEmRascunho");

        Status = DraftStatus.Sent;
        UpdatedAt = clock.UtcNow;

        _domainEvents.Add(new DraftSent(DraftId, sentBy, clock.UtcNow));
    }

    public void ClearDomainEvents()
    {
        _domainEvents.Clear();
    }
}





