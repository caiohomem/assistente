namespace AssistenteExecutivo.Domain.DomainEvents;

public record MilestoneCreated(
    Guid AgreementId,
    Guid MilestoneId,
    string Description,
    DateTime DueDate,
    decimal Value,
    string Currency,
    DateTime OccurredAt) : IDomainEvent;
