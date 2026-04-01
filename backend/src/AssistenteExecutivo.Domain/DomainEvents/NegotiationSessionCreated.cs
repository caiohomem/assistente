namespace AssistenteExecutivo.Domain.DomainEvents;

public record NegotiationSessionCreated(
    Guid SessionId,
    Guid OwnerUserId,
    string Title,
    DateTime OccurredAt) : IDomainEvent;
