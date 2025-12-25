using AssistenteExecutivo.Domain.DomainEvents;

namespace AssistenteExecutivo.Domain.DomainEvents;

public record PasswordResetRequested(
    Guid UserId,
    string Email,
    string Token,
    DateTime ExpiresAt,
    DateTime OccurredAt) : IDomainEvent;

