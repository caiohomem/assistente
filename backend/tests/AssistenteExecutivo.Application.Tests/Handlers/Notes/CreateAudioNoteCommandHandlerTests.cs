using AssistenteExecutivo.Application.Commands.Contacts;
using AssistenteExecutivo.Application.Commands.Notes;
using AssistenteExecutivo.Application.Queries.Notes;
using AssistenteExecutivo.Application.Tests.Helpers;
using AssistenteExecutivo.Domain.Exceptions;
using FluentAssertions;

namespace AssistenteExecutivo.Application.Tests.Handlers.Notes;

public class CreateAudioNoteCommandHandlerTests : HandlerTestBase
{
    [Fact]
    public async Task Handle_ValidCommand_ShouldCreateAudioNote()
    {
        // Arrange
        var ownerUserId = Guid.NewGuid();
        var contactId = await SendAsync(new CreateContactCommand
        {
            OwnerUserId = ownerUserId,
            FirstName = "João"
        });

        var command = new CreateAudioNoteCommand
        {
            ContactId = contactId,
            AuthorId = ownerUserId,
            Transcript = "This is a transcript from an audio note",
            StructuredData = "{\"duration\":120}"
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
        note.RawContent.Should().Be("This is a transcript from an audio note");
        note.Type.Should().Be(Domain.Enums.NoteType.Audio);
    }

    [Fact]
    public async Task Handle_NonExistentContact_ShouldThrowException()
    {
        // Arrange
        var ownerUserId = Guid.NewGuid();
        var command = new CreateAudioNoteCommand
        {
            ContactId = Guid.NewGuid(),
            AuthorId = ownerUserId,
            Transcript = "Test transcript"
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

        var command = new CreateAudioNoteCommand
        {
            ContactId = contactId,
            AuthorId = ownerUserId2, // Different owner
            Transcript = "Test transcript"
        };

        // Act & Assert
        var act = async () => await SendAsync(command);
        await act.Should().ThrowAsync<DomainException>()
            .WithMessage("*AuthorIdInvalido*");
    }
}

