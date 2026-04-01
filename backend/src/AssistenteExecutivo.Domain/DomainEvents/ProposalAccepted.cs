namespace AssistenteExecutivo.Domain.DomainEvents;

public record ProposalAccepted(
    Guid SessionId,
    Guid ProposalId,
    Guid? PartyId,
    DateTime OccurredAt) : IDomainEvent;
