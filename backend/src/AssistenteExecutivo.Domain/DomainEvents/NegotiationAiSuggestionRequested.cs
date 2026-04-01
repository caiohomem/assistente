namespace AssistenteExecutivo.Domain.DomainEvents;

public record NegotiationAiSuggestionRequested(
    Guid SessionId,
    Guid OwnerUserId,
    string? Instructions,
    DateTime OccurredAt) : IDomainEvent;
