using AssistenteExecutivo.Domain.Enums;

namespace AssistenteExecutivo.Domain.DomainEvents;

public record ProposalSubmitted(
    Guid SessionId,
    Guid ProposalId,
    ProposalSource Source,
    Guid? PartyId,
    DateTime OccurredAt) : IDomainEvent;
