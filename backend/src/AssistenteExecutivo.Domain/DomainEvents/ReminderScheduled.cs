using AssistenteExecutivo.Domain.Enums;

namespace AssistenteExecutivo.Domain.DomainEvents;

public record ReminderScheduled(
    Guid ReminderId,
    Guid OwnerUserId,
    Guid ContactId,
    string Reason,
    DateTime ScheduledFor,
    DateTime OccurredAt) : IDomainEvent;

