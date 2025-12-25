using AssistenteExecutivo.Domain.Enums;

namespace AssistenteExecutivo.Domain.DomainEvents;

public record UserLoggedIn(
    Guid UserId,
    AuthMethod AuthMethod,
    DateTime OccurredAt) : IDomainEvent;

