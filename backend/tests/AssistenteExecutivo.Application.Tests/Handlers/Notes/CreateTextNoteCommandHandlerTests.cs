using AssistenteExecutivo.Application.Commands.Contacts;
using AssistenteExecutivo.Application.Commands.Notes;
using AssistenteExecutivo.Application.Queries.Notes;
using AssistenteExecutivo.Application.Tests.Helpers;
using AssistenteExecutivo.Domain.Exceptions;
using FluentAssertions;

namespace AssistenteExecutivo.Application.Tests.Handlers.Notes;

public class CreateTextNoteCommandHandlerTests : HandlerTestBase
{
    [Fact]
    public async Task Handle_ValidCommand_ShouldCreateTextNote()
    {
        // Arrange
        var ownerUserId = Guid.NewGuid();
        var contactId = await SendAsync(new CreateContactCommand
        {
            OwnerUserId = ownerUserId,
            FirstName = "João"
        });

        var command = new CreateTextNoteCommand
        {
            ContactId = contactId,
            AuthorId = ownerUserId,
            Text = "This is a test note",
            StructuredData = "{\"key\":\"value\"}"
        };

        // Act
        var noteId = await SendAsync(command);

        // Assert
        noteId.Should().NotBeEmpty();

        // Verify note was saved
        var query = new GetNoteByIdQuery
        {
            NoteId = noteId,
            OwnerUserId = ownerUserId
        };
        var note = await SendAsync(query);

        note.Should().NotBeNull();
        note!.NoteId.Should().Be(noteId);
        note.ContactId.Should().Be(contactId);
        note.AuthorId.Should().Be(ownerUserId);
        note.RawContent.Should().Be("This is a test note");
        note.Type.Should().Be(Domain.Enums.NoteType.Text);
    }

    [Fact]
    public async Task Handle_NonExistentContact_ShouldThrowException()
    {
        // Arrange
        var ownerUserId = Guid.NewGuid();
        var command = new CreateTextNoteCommand
        {
            ContactId = Guid.NewGuid(),
            AuthorId = ownerUserId,
            Text = "Test note"
        };

        // Act & Assert
        var act = async () => await SendAsync(command);
        await act.Should().ThrowAsync<DomainException>()
            .WithMessage("*ContactIdObrigatorio*");
    }

    [Fact]
    public async Task Handle_DifferentAuthorId_ShouldThrowException()
    {
        // Arrange
        var ownerUserId1 = Guid.NewGuid();
        var ownerUserId2 = Guid.NewGuid();

        var contactId = await SendAsync(new CreateContactCommand
        {
            OwnerUserId = ownerUserId1,
            FirstName = "João"
        });

        var command = new CreateTextNoteCommand
        {
            ContactId = contactId,
            AuthorId = ownerUserId2, // Different owner
            Text = "Test note"
        };

        // Act & Assert
        var act = async () => await SendAsync(command);
        await act.Should().ThrowAsync<DomainException>()
            .WithMessage("*AuthorIdInvalido*");
    }

    [Fact]
    public async Task Handle_WithStructuredData_ShouldSaveStructuredData()
    {
        // Arrange
        var ownerUserId = Guid.NewGuid();
        var contactId = await SendAsync(new CreateContactCommand
        {
            OwnerUserId = ownerUserId,
            FirstName = "João"
        });

        var structuredData = "{\"meeting\":\"2024-01-15\",\"topics\":[\"topic1\",\"topic2\"]}";
        var command = new CreateTextNoteCommand
        {
            ContactId = contactId,
            AuthorId = ownerUserId,
            Text = "Meeting notes",
            StructuredData = structuredData
        };

        // Act
        var noteId = await SendAsync(command);

        // Assert
        var query = new GetNoteByIdQuery
        {
            NoteId = noteId,
            OwnerUserId = ownerUserId
        };
        var note = await SendAsync(query);

        note.Should().NotBeNull();
        note!.StructuredData.Should().Be(structuredData);
    }
}

