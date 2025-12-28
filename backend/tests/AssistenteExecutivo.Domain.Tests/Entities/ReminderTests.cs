using AssistenteExecutivo.Domain.Entities;
using AssistenteExecutivo.Domain.Enums;
using AssistenteExecutivo.Domain.Exceptions;
using AssistenteExecutivo.Domain.Interfaces;
using FluentAssertions;

namespace AssistenteExecutivo.Domain.Tests.Entities;

public class ReminderTests
{
    private readonly IClock _clock;

    public ReminderTests()
    {
        _clock = new TestClock();
    }

    [Fact]
    public void Create_ValidData_ShouldCreateReminder()
    {
        // Arrange
        var reminderId = Guid.NewGuid();
        var ownerUserId = Guid.NewGuid();
        var contactId = Guid.NewGuid();
        var reason = "Follow up meeting";
        var scheduledFor = _clock.UtcNow.AddDays(1);

        // Act
        var reminder = Reminder.Create(reminderId, ownerUserId, contactId, reason, scheduledFor, _clock);

        // Assert
        reminder.ReminderId.Should().Be(reminderId);
        reminder.OwnerUserId.Should().Be(ownerUserId);
        reminder.ContactId.Should().Be(contactId);
        reminder.Reason.Should().Be(reason);
        reminder.ScheduledFor.Should().Be(scheduledFor);
        reminder.Status.Should().Be(ReminderStatus.Pending);
        reminder.DomainEvents.Should().HaveCount(1);
        reminder.DomainEvents.First().Should().BeOfType<Domain.DomainEvents.ReminderScheduled>();
    }

    [Fact]
    public void Create_EmptyReminderId_ShouldThrow()
    {
        // Arrange
        var ownerUserId = Guid.NewGuid();
        var contactId = Guid.NewGuid();
        var reason = "Follow up";
        var scheduledFor = _clock.UtcNow.AddDays(1);

        // Act & Assert
        var act = () => Reminder.Create(Guid.Empty, ownerUserId, contactId, reason, scheduledFor, _clock);
        act.Should().Throw<DomainException>()
            .WithMessage("*ReminderIdObrigatorio*");
    }

    [Fact]
    public void Create_EmptyOwnerUserId_ShouldThrow()
    {
        // Arrange
        var reminderId = Guid.NewGuid();
        var contactId = Guid.NewGuid();
        var reason = "Follow up";
        var scheduledFor = _clock.UtcNow.AddDays(1);

        // Act & Assert
        var act = () => Reminder.Create(reminderId, Guid.Empty, contactId, reason, scheduledFor, _clock);
        act.Should().Throw<DomainException>()
            .WithMessage("*OwnerUserIdObrigatorio*");
    }

    [Fact]
    public void Create_EmptyContactId_ShouldThrow()
    {
        // Arrange
        var reminderId = Guid.NewGuid();
        var ownerUserId = Guid.NewGuid();
        var reason = "Follow up";
        var scheduledFor = _clock.UtcNow.AddDays(1);

        // Act & Assert
        var act = () => Reminder.Create(reminderId, ownerUserId, Guid.Empty, reason, scheduledFor, _clock);
        act.Should().Throw<DomainException>()
            .WithMessage("*ContactIdObrigatorio*");
    }

    [Fact]
    public void Create_EmptyReason_ShouldThrow()
    {
        // Arrange
        var reminderId = Guid.NewGuid();
        var ownerUserId = Guid.NewGuid();
        var contactId = Guid.NewGuid();
        var scheduledFor = _clock.UtcNow.AddDays(1);

        // Act & Assert
        var act = () => Reminder.Create(reminderId, ownerUserId, contactId, "", scheduledFor, _clock);
        act.Should().Throw<DomainException>()
            .WithMessage("*ReminderReasonObrigatorio*");
    }

    [Fact]
    public void Create_PastScheduledFor_ShouldThrow()
    {
        // Arrange
        var reminderId = Guid.NewGuid();
        var ownerUserId = Guid.NewGuid();
        var contactId = Guid.NewGuid();
        var reason = "Follow up";
        var scheduledFor = _clock.UtcNow.AddDays(-1);

        // Act & Assert
        var act = () => Reminder.Create(reminderId, ownerUserId, contactId, reason, scheduledFor, _clock);
        act.Should().Throw<DomainException>()
            .WithMessage("*ReminderScheduledForDeveSerFuturo*");
    }

    [Fact]
    public void UpdateSuggestedMessage_ShouldUpdateMessage()
    {
        // Arrange
        var reminder = CreateReminder();
        var newMessage = "New suggested message";

        // Act
        reminder.UpdateSuggestedMessage(newMessage, _clock);

        // Assert
        reminder.SuggestedMessage.Should().Be(newMessage);
    }

    [Fact]
    public void UpdateSuggestedMessage_Null_ShouldSetToNull()
    {
        // Arrange
        var reminder = CreateReminder();
        reminder.UpdateSuggestedMessage("Initial message", _clock);

        // Act
        reminder.UpdateSuggestedMessage(null, _clock);

        // Assert
        reminder.SuggestedMessage.Should().BeNull();
    }

    [Fact]
    public void MarkAsSent_WhenPending_ShouldChangeStatus()
    {
        // Arrange
        var reminder = CreateReminder();

        // Act
        reminder.MarkAsSent(_clock);

        // Assert
        reminder.Status.Should().Be(ReminderStatus.Sent);
        reminder.DomainEvents.Should().HaveCount(1);
        reminder.DomainEvents.First().Should().BeOfType<Domain.DomainEvents.ReminderStatusChanged>();
    }

    [Fact]
    public void MarkAsSent_WhenSnoozed_ShouldChangeStatus()
    {
        // Arrange
        var reminder = CreateReminder();
        reminder.Snooze(_clock.UtcNow.AddDays(2), _clock);
        reminder.ClearDomainEvents();

        // Act
        reminder.MarkAsSent(_clock);

        // Assert
        reminder.Status.Should().Be(ReminderStatus.Sent);
        reminder.DomainEvents.Should().HaveCount(1);
    }

    [Fact]
    public void MarkAsSent_WhenSent_ShouldThrow()
    {
        // Arrange
        var reminder = CreateReminder();
        reminder.MarkAsSent(_clock);
        reminder.ClearDomainEvents();

        // Act & Assert
        var act = () => reminder.MarkAsSent(_clock);
        act.Should().Throw<DomainException>()
            .WithMessage("*ReminderSoPodeSerMarcadoComoEnviadoSePendenteOuAdiado*");
    }

    [Fact]
    public void Dismiss_WhenPending_ShouldChangeStatus()
    {
        // Arrange
        var reminder = CreateReminder();

        // Act
        reminder.Dismiss(_clock);

        // Assert
        reminder.Status.Should().Be(ReminderStatus.Dismissed);
        reminder.DomainEvents.Should().HaveCount(1);
    }

    [Fact]
    public void Dismiss_WhenSent_ShouldThrow()
    {
        // Arrange
        var reminder = CreateReminder();
        reminder.MarkAsSent(_clock);
        reminder.ClearDomainEvents();

        // Act & Assert
        var act = () => reminder.Dismiss(_clock);
        act.Should().Throw<DomainException>()
            .WithMessage("*ReminderEnviadoNaoPodeSerDescartado*");
    }

    [Fact]
    public void Snooze_WhenPending_ShouldChangeStatus()
    {
        // Arrange
        var reminder = CreateReminder();
        var newScheduledFor = _clock.UtcNow.AddDays(3);
        reminder.ClearDomainEvents();

        // Act
        reminder.Snooze(newScheduledFor, _clock);

        // Assert
        reminder.Status.Should().Be(ReminderStatus.Snoozed);
        reminder.ScheduledFor.Should().Be(newScheduledFor);
        reminder.DomainEvents.Should().HaveCount(1);
    }

    [Fact]
    public void Snooze_WhenNotPending_ShouldThrow()
    {
        // Arrange
        var reminder = CreateReminder();
        reminder.MarkAsSent(_clock);
        reminder.ClearDomainEvents();

        // Act & Assert
        var act = () => reminder.Snooze(_clock.UtcNow.AddDays(1), _clock);
        act.Should().Throw<DomainException>()
            .WithMessage("*ReminderSoPodeSerAdiadoSePendente*");
    }

    [Fact]
    public void Snooze_WithPastDate_ShouldThrow()
    {
        // Arrange
        var reminder = CreateReminder();
        reminder.ClearDomainEvents();

        // Act & Assert
        var act = () => reminder.Snooze(_clock.UtcNow.AddDays(-1), _clock);
        act.Should().Throw<DomainException>()
            .WithMessage("*ReminderScheduledForDeveSerFuturo*");
    }

    [Fact]
    public void ClearDomainEvents_ShouldClearAllEvents()
    {
        // Arrange
        var reminder = CreateReminder();
        reminder.DomainEvents.Should().HaveCount(1);

        // Act
        reminder.ClearDomainEvents();

        // Assert
        reminder.DomainEvents.Should().BeEmpty();
    }

    private Reminder CreateReminder()
    {
        var reminderId = Guid.NewGuid();
        var ownerUserId = Guid.NewGuid();
        var contactId = Guid.NewGuid();
        var reason = "Follow up meeting";
        var scheduledFor = _clock.UtcNow.AddDays(1);
        return Reminder.Create(reminderId, ownerUserId, contactId, reason, scheduledFor, _clock);
    }

    private class TestClock : IClock
    {
        public DateTime UtcNow => DateTime.UtcNow;
    }
}





