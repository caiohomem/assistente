using AssistenteExecutivo.Application.Commands.Contacts;
using AssistenteExecutivo.Application.Queries.Contacts;
using AssistenteExecutivo.Application.Tests.Helpers;
using FluentAssertions;

namespace AssistenteExecutivo.Application.Tests.Handlers.Contacts;

public class GetContactByIdQueryHandlerTests : HandlerTestBase
{
    [Fact]
    public async Task Handle_ExistingContact_ShouldReturnContact()
    {
        // Arrange
        var ownerUserId = Guid.NewGuid();
        var createCommand = new CreateContactCommand
        {
            OwnerUserId = ownerUserId,
            FirstName = "João",
            LastName = "Silva"
        };
        var contactId = await SendAsync(createCommand);

        // Act
        var query = new GetContactByIdQuery
        {
            ContactId = contactId,
            OwnerUserId = ownerUserId
        };
        var result = await SendAsync(query);

        // Assert
        result.Should().NotBeNull();
        result!.ContactId.Should().Be(contactId);
        result.FirstName.Should().Be("João");
        result.LastName.Should().Be("Silva");
        result.OwnerUserId.Should().Be(ownerUserId);
    }

    [Fact]
    public async Task Handle_NonExistentContact_ShouldReturnNull()
    {
        // Arrange
        var ownerUserId = Guid.NewGuid();
        var query = new GetContactByIdQuery
        {
            ContactId = Guid.NewGuid(),
            OwnerUserId = ownerUserId
        };

        // Act
        var result = await SendAsync(query);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task Handle_ContactFromDifferentOwner_ShouldReturnNull()
    {
        // Arrange
        var ownerUserId1 = Guid.NewGuid();
        var ownerUserId2 = Guid.NewGuid();

        var createCommand = new CreateContactCommand
        {
            OwnerUserId = ownerUserId1,
            FirstName = "João"
        };
        var contactId = await SendAsync(createCommand);

        // Act
        var query = new GetContactByIdQuery
        {
            ContactId = contactId,
            OwnerUserId = ownerUserId2 // Different owner
        };
        var result = await SendAsync(query);

        // Assert
        result.Should().BeNull();
    }
}














