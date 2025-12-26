using AssistenteExecutivo.Application.Commands.Capture;
using AssistenteExecutivo.Application.Queries.Contacts;
using AssistenteExecutivo.Application.Tests.Helpers;
using FluentAssertions;

namespace AssistenteExecutivo.Application.Tests.Handlers.Capture;

public class UploadCardCommandHandlerTests : HandlerTestBase
{
    [Fact]
    public async Task Handle_ValidCardImage_ShouldCreateContactAndJob()
    {
        // Arrange
        var ownerUserId = Guid.NewGuid();
        var imageBytes = new byte[] { 1, 2, 3, 4, 5 };
        var command = new UploadCardCommand
        {
            OwnerUserId = ownerUserId,
            ImageBytes = imageBytes,
            FileName = "business-card.jpg",
            MimeType = "image/jpeg"
        };

        // Act
        var result = await SendAsync(command);

        // Assert
        result.Should().NotBeNull();
        result.ContactId.Should().NotBeEmpty();
        result.JobId.Should().NotBeEmpty();
        result.MediaId.Should().NotBeEmpty();

        // Verify contact was created
        var contactQuery = new GetContactByIdQuery
        {
            ContactId = result.ContactId,
            OwnerUserId = ownerUserId
        };
        var contact = await SendAsync(contactQuery);

        contact.Should().NotBeNull();
        contact!.FirstName.Should().Be("João");
        contact.LastName.Should().Be("Silva");
    }

    [Fact]
    public async Task Handle_ExistingContactByEmail_ShouldUpdateContact()
    {
        // Arrange
        var ownerUserId = Guid.NewGuid();

        // Create existing contact with same email
        var existingContactId = await SendAsync(new Application.Commands.Contacts.CreateContactCommand
        {
            OwnerUserId = ownerUserId,
            FirstName = "Existing",
            LastName = "Contact"
        });

        // Add email to existing contact
        await SendAsync(new Application.Commands.Contacts.AddContactEmailCommand
        {
            ContactId = existingContactId,
            OwnerUserId = ownerUserId,
            Email = "joao.silva@example.com"
        });

        var imageBytes = new byte[] { 1, 2, 3, 4, 5 };
        var command = new UploadCardCommand
        {
            OwnerUserId = ownerUserId,
            ImageBytes = imageBytes,
            FileName = "business-card.jpg",
            MimeType = "image/jpeg"
        };

        // Act
        var result = await SendAsync(command);

        // Assert
        result.Should().NotBeNull();
        // Should return the existing contact ID
        result.ContactId.Should().Be(existingContactId);
    }
}



