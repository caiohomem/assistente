namespace AssistenteExecutivo.Domain.DomainEvents;

public record CaptureJobFailed(
    Guid JobId,
    string ErrorCode,
    DateTime OccurredAt) : IDomainEvent;

