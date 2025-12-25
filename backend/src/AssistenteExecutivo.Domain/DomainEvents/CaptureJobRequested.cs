using AssistenteExecutivo.Domain.Enums;

namespace AssistenteExecutivo.Domain.DomainEvents;

public record CaptureJobRequested(
    Guid JobId,
    JobType Type,
    DateTime OccurredAt) : IDomainEvent;

