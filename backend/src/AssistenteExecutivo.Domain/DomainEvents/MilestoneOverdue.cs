namespace AssistenteExecutivo.Domain.DomainEvents;

public record MilestoneOverdue(
    Guid AgreementId,
    Guid MilestoneId,
    DateTime DueDate,
    DateTime OccurredAt) : IDomainEvent;
