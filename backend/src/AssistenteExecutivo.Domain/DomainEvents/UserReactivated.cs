namespace AssistenteExecutivo.Domain.DomainEvents;

public record UserReactivated(
    Guid UserId,
    DateTime OccurredAt) : IDomainEvent;

