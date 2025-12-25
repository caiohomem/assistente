using AssistenteExecutivo.Domain.Enums;

namespace AssistenteExecutivo.Domain.DomainEvents;

public record MediaUploaded(
    Guid MediaId,
    MediaKind Kind,
    DateTime OccurredAt) : IDomainEvent;

