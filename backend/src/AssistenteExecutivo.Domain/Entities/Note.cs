using AssistenteExecutivo.Domain.DomainEvents;
using AssistenteExecutivo.Domain.Enums;
using AssistenteExecutivo.Domain.Exceptions;
using AssistenteExecutivo.Domain.Interfaces;

namespace AssistenteExecutivo.Domain.Entities;

public class Note
{
    private readonly List<IDomainEvent> _domainEvents = new();

    private Note() { } // EF Core

    public Note(
        Guid noteId,
        Guid contactId,
        Guid authorId,
        NoteType type,
        string rawContent,
        IClock clock)
    {
        if (noteId == Guid.Empty)
            throw new DomainException("Domain:NoteIdObrigatorio");

        if (contactId == Guid.Empty)
            throw new DomainException("Domain:ContactIdObrigatorio");

        if (authorId == Guid.Empty)
            throw new DomainException("Domain:AuthorIdObrigatorio");

        if (string.IsNullOrWhiteSpace(rawContent))
            throw new DomainException("Domain:RawContentObrigatorio");

        if (clock == null)
            throw new DomainException("Domain:ClockObrigatorio");

        NoteId = noteId;
        ContactId = contactId;
        AuthorId = authorId;
        Type = type;
        RawContent = rawContent;
        Version = 1;
        CreatedAt = clock.UtcNow;
        UpdatedAt = clock.UtcNow;
    }

    public Guid NoteId { get; private set; }
    public Guid ContactId { get; private set; }
    public Guid AuthorId { get; private set; }
    public NoteType Type { get; private set; }
    public string RawContent { get; private set; } = null!;
    public string? StructuredData { get; private set; } // JSONB
    public int Version { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime UpdatedAt { get; private set; }

    public IReadOnlyCollection<IDomainEvent> DomainEvents => _domainEvents.AsReadOnly();

    public static Note CreateTextNote(
        Guid noteId,
        Guid contactId,
        Guid authorId,
        string text,
        IClock clock)
    {
        var note = new Note(noteId, contactId, authorId, NoteType.Text, text, clock);
        note._domainEvents.Add(new NoteCreated(note.NoteId, note.ContactId, note.AuthorId, NoteType.Text, clock.UtcNow));
        return note;
    }

    public static Note CreateAudioNote(
        Guid noteId,
        Guid contactId,
        Guid authorId,
        string transcript,
        IClock clock)
    {
        var note = new Note(noteId, contactId, authorId, NoteType.Audio, transcript, clock);
        note._domainEvents.Add(new NoteCreated(note.NoteId, note.ContactId, note.AuthorId, NoteType.Audio, clock.UtcNow));
        return note;
    }

    public void UpdateStructuredData(string structuredDataJson, IClock clock)
    {
        if (string.IsNullOrWhiteSpace(structuredDataJson))
            throw new DomainException("Domain:StructuredDataObrigatorio");

        StructuredData = structuredDataJson;
        Version++;
        UpdatedAt = clock.UtcNow;
    }

    public void UpdateRawContent(string rawContent, IClock clock)
    {
        if (string.IsNullOrWhiteSpace(rawContent))
            throw new DomainException("Domain:RawContentObrigatorio");

        RawContent = rawContent;
        Version++;
        UpdatedAt = clock.UtcNow;
    }

    public void ClearDomainEvents()
    {
        _domainEvents.Clear();
    }
}


