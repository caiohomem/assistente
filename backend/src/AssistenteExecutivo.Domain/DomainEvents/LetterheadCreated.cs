namespace AssistenteExecutivo.Domain.DomainEvents;

public record LetterheadCreated(
    Guid LetterheadId,
    Guid OwnerUserId,
    string Name,
    DateTime OccurredAt) : IDomainEvent;







