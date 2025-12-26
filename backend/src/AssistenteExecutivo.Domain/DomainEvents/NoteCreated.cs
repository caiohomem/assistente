using AssistenteExecutivo.Domain.Enums;

namespace AssistenteExecutivo.Domain.DomainEvents;

public record NoteCreated(
    Guid NoteId,
    Guid ContactId,
    Guid AuthorId,
    NoteType Type,
    DateTime OccurredAt) : IDomainEvent;



