using AssistenteExecutivo.Application.Commands.Automation;
using AssistenteExecutivo.Application.Commands.Contacts;
using AssistenteExecutivo.Application.Queries.Automation;
using AssistenteExecutivo.Application.Tests.Helpers;
using AssistenteExecutivo.Domain.Exceptions;
using FluentAssertions;

namespace AssistenteExecutivo.Application.Tests.Handlers.Automation;

public class CreateReminderCommandHandlerTests : HandlerTestBase
{
    [Fact]
    public async Task Handle_ValidCommand_ShouldCreateReminder()
    {
        // Arrange
        var ownerUserId = Guid.NewGuid();

        // Create a contact first
        var contactCommand = new CreateContactCommand
        {
            OwnerUserId = ownerUserId,
            FirstName = "João",
            LastName = "Silva"
        };
        var contactId = await SendAsync(contactCommand);

        var command = new CreateReminderCommand
        {
            OwnerUserId = ownerUserId,
            ContactId = contactId,
            Reason = "Follow up meeting",
            SuggestedMessage = "Don't forget about our meeting",
            ScheduledFor = Clock.UtcNow.AddDays(1)
        };

        // Act
        var reminderId = await SendAsync(command);

        // Assert
        reminderId.Should().NotBeEmpty();

        // Verify reminder was saved
        var query = new GetReminderByIdQuery
        {
            ReminderId = reminderId,
            OwnerUserId = ownerUserId
        };
        var reminder = await SendAsync(query);

        reminder.Should().NotBeNull();
        reminder!.Reason.Should().Be("Follow up meeting");
        reminder.SuggestedMessage.Should().Be("Don't forget about our meeting");
        reminder.ContactId.Should().Be(contactId);
    }

    [Fact]
    public async Task Handle_EmptyOwnerUserId_ShouldThrowException()
    {
        // Arrange
        var command = new CreateReminderCommand
        {
            OwnerUserId = Guid.Empty,
            ContactId = Guid.NewGuid(),
            Reason = "Follow up",
            ScheduledFor = Clock.UtcNow.AddDays(1)
        };

        // Act & Assert
        var act = async () => await SendAsync(command);
        await act.Should().ThrowAsync<DomainException>()
            .WithMessage("*OwnerUserIdObrigatorio*");
    }

    [Fact]
    public async Task Handle_ContactNotFound_ShouldThrowException()
    {
        // Arrange
        var ownerUserId = Guid.NewGuid();
        var command = new CreateReminderCommand
        {
            OwnerUserId = ownerUserId,
            ContactId = Guid.NewGuid(), // Non-existent contact
            Reason = "Follow up",
            ScheduledFor = Clock.UtcNow.AddDays(1)
        };

        // Act & Assert
        var act = async () => await SendAsync(command);
        await act.Should().ThrowAsync<DomainException>()
            .WithMessage("*ContactNaoEncontrado*");
    }

    [Fact]
    public async Task Handle_PastScheduledFor_ShouldThrowException()
    {
        // Arrange
        var ownerUserId = Guid.NewGuid();

        var contactCommand = new CreateContactCommand
        {
            OwnerUserId = ownerUserId,
            FirstName = "João"
        };
        var contactId = await SendAsync(contactCommand);

        var command = new CreateReminderCommand
        {
            OwnerUserId = ownerUserId,
            ContactId = contactId,
            Reason = "Follow up",
            ScheduledFor = Clock.UtcNow.AddDays(-1) // Past date
        };

        // Act & Assert
        var act = async () => await SendAsync(command);
        await act.Should().ThrowAsync<DomainException>()
            .WithMessage("*ReminderScheduledForDeveSerFuturo*");
    }
}









