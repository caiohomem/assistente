namespace AssistenteExecutivo.Domain.DomainEvents;

public record MilestoneCompleted(
    Guid AgreementId,
    Guid MilestoneId,
    string? Notes,
    DateTime OccurredAt) : IDomainEvent;
