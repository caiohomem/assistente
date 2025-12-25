using AssistenteExecutivo.Domain.Enums;

namespace AssistenteExecutivo.Domain.DomainEvents;

public record CaptureJobCompleted(
    Guid JobId,
    JobType Type,
    DateTime OccurredAt) : IDomainEvent;

