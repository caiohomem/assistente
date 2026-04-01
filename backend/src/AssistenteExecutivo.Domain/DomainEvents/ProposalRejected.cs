namespace AssistenteExecutivo.Domain.DomainEvents;

public record ProposalRejected(
    Guid SessionId,
    Guid ProposalId,
    Guid? PartyId,
    string Reason,
    DateTime OccurredAt) : IDomainEvent;
