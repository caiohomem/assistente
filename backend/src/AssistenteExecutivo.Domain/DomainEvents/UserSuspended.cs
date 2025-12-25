namespace AssistenteExecutivo.Domain.DomainEvents;

public record UserSuspended(
    Guid UserId,
    string Reason,
    DateTime OccurredAt) : IDomainEvent;

