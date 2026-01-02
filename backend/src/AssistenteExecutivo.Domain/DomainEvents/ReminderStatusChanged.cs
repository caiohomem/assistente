using AssistenteExecutivo.Domain.Enums;

namespace AssistenteExecutivo.Domain.DomainEvents;

public record ReminderStatusChanged(
    Guid ReminderId,
    ReminderStatus OldStatus,
    ReminderStatus NewStatus,
    DateTime OccurredAt) : IDomainEvent;









