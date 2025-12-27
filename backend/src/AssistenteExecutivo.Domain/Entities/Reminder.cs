using AssistenteExecutivo.Domain.DomainEvents;
using AssistenteExecutivo.Domain.Enums;
using AssistenteExecutivo.Domain.Exceptions;
using AssistenteExecutivo.Domain.Interfaces;

namespace AssistenteExecutivo.Domain.Entities;

public class Reminder
{
    private readonly List<IDomainEvent> _domainEvents = new();

    private Reminder() { } // EF Core

    public Reminder(
        Guid reminderId,
        Guid ownerUserId,
        Guid contactId,
        string reason,
        DateTime scheduledFor,
        IClock clock)
    {
        if (reminderId == Guid.Empty)
            throw new DomainException("Domain:ReminderIdObrigatorio");

        if (ownerUserId == Guid.Empty)
            throw new DomainException("Domain:OwnerUserIdObrigatorio");

        if (contactId == Guid.Empty)
            throw new DomainException("Domain:ContactIdObrigatorio");

        if (string.IsNullOrWhiteSpace(reason))
            throw new DomainException("Domain:ReminderReasonObrigatorio");

        if (scheduledFor < clock.UtcNow)
            throw new DomainException("Domain:ReminderScheduledForDeveSerFuturo");

        ReminderId = reminderId;
        OwnerUserId = ownerUserId;
        ContactId = contactId;
        Reason = reason.Trim();
        ScheduledFor = scheduledFor;
        Status = ReminderStatus.Pending;
        CreatedAt = clock.UtcNow;
        UpdatedAt = clock.UtcNow;
    }

    public Guid ReminderId { get; private set; }
    public Guid OwnerUserId { get; private set; }
    public Guid ContactId { get; private set; }
    public string Reason { get; private set; } = null!;
    public string? SuggestedMessage { get; private set; }
    public DateTime ScheduledFor { get; private set; }
    public ReminderStatus Status { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime UpdatedAt { get; private set; }

    public IReadOnlyCollection<IDomainEvent> DomainEvents => _domainEvents.AsReadOnly();

    public static Reminder Create(
        Guid reminderId,
        Guid ownerUserId,
        Guid contactId,
        string reason,
        DateTime scheduledFor,
        IClock clock)
    {
        var reminder = new Reminder(reminderId, ownerUserId, contactId, reason, scheduledFor, clock);
        reminder._domainEvents.Add(new ReminderScheduled(
            reminder.ReminderId,
            reminder.OwnerUserId,
            reminder.ContactId,
            reminder.Reason,
            reminder.ScheduledFor,
            clock.UtcNow));
        return reminder;
    }

    public void UpdateSuggestedMessage(string? suggestedMessage, IClock clock)
    {
        SuggestedMessage = suggestedMessage?.Trim();
        UpdatedAt = clock.UtcNow;
    }

    public void MarkAsSent(IClock clock)
    {
        if (Status != ReminderStatus.Pending && Status != ReminderStatus.Snoozed)
            throw new DomainException("Domain:ReminderSoPodeSerMarcadoComoEnviadoSePendenteOuAdiado");

        var oldStatus = Status;
        Status = ReminderStatus.Sent;
        UpdatedAt = clock.UtcNow;

        _domainEvents.Add(new ReminderStatusChanged(ReminderId, oldStatus, Status, clock.UtcNow));
    }

    public void Dismiss(IClock clock)
    {
        if (Status == ReminderStatus.Sent)
            throw new DomainException("Domain:ReminderEnviadoNaoPodeSerDescartado");

        var oldStatus = Status;
        Status = ReminderStatus.Dismissed;
        UpdatedAt = clock.UtcNow;

        _domainEvents.Add(new ReminderStatusChanged(ReminderId, oldStatus, Status, clock.UtcNow));
    }

    public void Snooze(DateTime newScheduledFor, IClock clock)
    {
        if (Status != ReminderStatus.Pending)
            throw new DomainException("Domain:ReminderSoPodeSerAdiadoSePendente");

        if (newScheduledFor < clock.UtcNow)
            throw new DomainException("Domain:ReminderScheduledForDeveSerFuturo");

        var oldStatus = Status;
        Status = ReminderStatus.Snoozed;
        ScheduledFor = newScheduledFor;
        UpdatedAt = clock.UtcNow;

        _domainEvents.Add(new ReminderStatusChanged(ReminderId, oldStatus, Status, clock.UtcNow));
    }

    public void ClearDomainEvents()
    {
        _domainEvents.Clear();
    }
}

