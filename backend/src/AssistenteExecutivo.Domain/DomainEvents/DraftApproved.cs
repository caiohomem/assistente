namespace AssistenteExecutivo.Domain.DomainEvents;

public record DraftApproved(
    Guid DraftId,
    Guid ApprovedBy,
    DateTime OccurredAt) : IDomainEvent;

