using AssistenteExecutivo.Application.Commands.Automation;
using AssistenteExecutivo.Application.Commands.Contacts;
using AssistenteExecutivo.Application.Queries.Automation;
using AssistenteExecutivo.Application.Tests.Helpers;
using AssistenteExecutivo.Domain.Enums;
using AssistenteExecutivo.Domain.Exceptions;
using FluentAssertions;

namespace AssistenteExecutivo.Application.Tests.Handlers.Automation;

public class UpdateReminderStatusCommandHandlerTests : HandlerTestBase
{
    [Fact]
    public async Task Handle_MarkAsSent_ShouldUpdateStatus()
    {
        // Arrange
        var ownerUserId = Guid.NewGuid();
        
        var contactCommand = new CreateContactCommand
        {
            OwnerUserId = ownerUserId,
            FirstName = "João"
        };
        var contactId = await SendAsync(contactCommand);

        var createCommand = new CreateReminderCommand
        {
            OwnerUserId = ownerUserId,
            ContactId = contactId,
            Reason = "Follow up",
            ScheduledFor = Clock.UtcNow.AddDays(1)
        };
        var reminderId = await SendAsync(createCommand);

        var updateCommand = new UpdateReminderStatusCommand
        {
            ReminderId = reminderId,
            OwnerUserId = ownerUserId,
            NewStatus = ReminderStatus.Sent
        };

        // Act
        await SendAsync(updateCommand);

        // Assert
        var query = new GetReminderByIdQuery
        {
            ReminderId = reminderId,
            OwnerUserId = ownerUserId
        };
        var reminder = await SendAsync(query);

        reminder.Should().NotBeNull();
        reminder!.Status.Should().Be(ReminderStatus.Sent);
    }

    [Fact]
    public async Task Handle_Dismiss_ShouldUpdateStatus()
    {
        // Arrange
        var ownerUserId = Guid.NewGuid();
        
        var contactCommand = new CreateContactCommand
        {
            OwnerUserId = ownerUserId,
            FirstName = "João"
        };
        var contactId = await SendAsync(contactCommand);

        var createCommand = new CreateReminderCommand
        {
            OwnerUserId = ownerUserId,
            ContactId = contactId,
            Reason = "Follow up",
            ScheduledFor = Clock.UtcNow.AddDays(1)
        };
        var reminderId = await SendAsync(createCommand);

        var updateCommand = new UpdateReminderStatusCommand
        {
            ReminderId = reminderId,
            OwnerUserId = ownerUserId,
            NewStatus = ReminderStatus.Dismissed
        };

        // Act
        await SendAsync(updateCommand);

        // Assert
        var query = new GetReminderByIdQuery
        {
            ReminderId = reminderId,
            OwnerUserId = ownerUserId
        };
        var reminder = await SendAsync(query);

        reminder.Should().NotBeNull();
        reminder!.Status.Should().Be(ReminderStatus.Dismissed);
    }

    [Fact]
    public async Task Handle_Snooze_ShouldUpdateStatusAndDate()
    {
        // Arrange
        var ownerUserId = Guid.NewGuid();
        
        var contactCommand = new CreateContactCommand
        {
            OwnerUserId = ownerUserId,
            FirstName = "João"
        };
        var contactId = await SendAsync(contactCommand);

        var createCommand = new CreateReminderCommand
        {
            OwnerUserId = ownerUserId,
            ContactId = contactId,
            Reason = "Follow up",
            ScheduledFor = Clock.UtcNow.AddDays(1)
        };
        var reminderId = await SendAsync(createCommand);

        var newScheduledFor = Clock.UtcNow.AddDays(3);
        var updateCommand = new UpdateReminderStatusCommand
        {
            ReminderId = reminderId,
            OwnerUserId = ownerUserId,
            NewStatus = ReminderStatus.Snoozed,
            NewScheduledFor = newScheduledFor
        };

        // Act
        await SendAsync(updateCommand);

        // Assert
        var query = new GetReminderByIdQuery
        {
            ReminderId = reminderId,
            OwnerUserId = ownerUserId
        };
        var reminder = await SendAsync(query);

        reminder.Should().NotBeNull();
        reminder!.Status.Should().Be(ReminderStatus.Snoozed);
        reminder.ScheduledFor.Should().BeCloseTo(newScheduledFor, TimeSpan.FromSeconds(1));
    }

    [Fact]
    public async Task Handle_ReminderNotFound_ShouldThrowException()
    {
        // Arrange
        var ownerUserId = Guid.NewGuid();
        var updateCommand = new UpdateReminderStatusCommand
        {
            ReminderId = Guid.NewGuid(),
            OwnerUserId = ownerUserId,
            NewStatus = ReminderStatus.Sent
        };

        // Act & Assert
        var act = async () => await SendAsync(updateCommand);
        await act.Should().ThrowAsync<DomainException>()
            .WithMessage("*ReminderNaoEncontrado*");
    }

    [Fact]
    public async Task Handle_SnoozeWithoutNewScheduledFor_ShouldThrowException()
    {
        // Arrange
        var ownerUserId = Guid.NewGuid();
        
        var contactCommand = new CreateContactCommand
        {
            OwnerUserId = ownerUserId,
            FirstName = "João"
        };
        var contactId = await SendAsync(contactCommand);

        var createCommand = new CreateReminderCommand
        {
            OwnerUserId = ownerUserId,
            ContactId = contactId,
            Reason = "Follow up",
            ScheduledFor = Clock.UtcNow.AddDays(1)
        };
        var reminderId = await SendAsync(createCommand);

        var updateCommand = new UpdateReminderStatusCommand
        {
            ReminderId = reminderId,
            OwnerUserId = ownerUserId,
            NewStatus = ReminderStatus.Snoozed
            // NewScheduledFor is null
        };

        // Act & Assert
        var act = async () => await SendAsync(updateCommand);
        await act.Should().ThrowAsync<DomainException>()
            .WithMessage("*NewScheduledForObrigatorioParaSnoozed*");
    }
}

