using AssistenteExecutivo.Application.Commands.Capture;
using AssistenteExecutivo.Application.Commands.Contacts;
using AssistenteExecutivo.Application.Queries.Notes;
using AssistenteExecutivo.Application.Tests.Helpers;
using FluentAssertions;

namespace AssistenteExecutivo.Application.Tests.Handlers.Capture;

public class ProcessAudioNoteCommandHandlerTests : HandlerTestBase
{
    [Fact]
    public async Task Handle_ValidAudio_ShouldCreateNoteAndJob()
    {
        // Arrange
        var ownerUserId = Guid.NewGuid();
        var contactId = await SendAsync(new CreateContactCommand
        {
            OwnerUserId = ownerUserId,
            FirstName = "João"
        });

        // Grant credits first (required for audio processing)
        await SendAsync(new Application.Commands.Credits.GrantCreditsCommand
        {
            OwnerUserId = ownerUserId,
            Amount = 10m,
            Reason = "Test credits"
        });

        var audioBytes = new byte[] { 1, 2, 3, 4, 5 };
        var command = new ProcessAudioNoteCommand
        {
            OwnerUserId = ownerUserId,
            ContactId = contactId,
            AudioBytes = audioBytes,
            FileName = "audio-note.mp3",
            MimeType = "audio/mpeg"
        };

        // Act
        var result = await SendAsync(command);

        // Assert
        result.Should().NotBeNull();
        result.NoteId.Should().NotBeEmpty();
        result.JobId.Should().NotBeEmpty();
        result.MediaId.Should().NotBeEmpty();

        // Verify note was created
        var noteQuery = new Application.Queries.Notes.GetNoteByIdQuery
        {
            NoteId = result.NoteId,
            OwnerUserId = ownerUserId
        };
        var note = await SendAsync(noteQuery);

        note.Should().NotBeNull();
        note!.Type.Should().Be(Domain.Enums.NoteType.Audio);
        note.RawContent.Should().Contain("mock transcript");
    }

    [Fact]
    public async Task Handle_NonExistentContact_ShouldThrowException()
    {
        // Arrange
        var ownerUserId = Guid.NewGuid();
        var audioBytes = new byte[] { 1, 2, 3, 4, 5 };
        var command = new ProcessAudioNoteCommand
        {
            OwnerUserId = ownerUserId,
            ContactId = Guid.NewGuid(), // Non-existent contact
            AudioBytes = audioBytes,
            FileName = "audio-note.mp3",
            MimeType = "audio/mpeg"
        };

        // Act & Assert
        var act = async () => await SendAsync(command);
        await act.Should().ThrowAsync<InvalidOperationException>();
    }

    [Fact]
    public async Task Handle_InsufficientCredits_ShouldThrowException()
    {
        // Arrange
        var ownerUserId = Guid.NewGuid();
        var contactId = await SendAsync(new CreateContactCommand
        {
            OwnerUserId = ownerUserId,
            FirstName = "João"
        });

        // Don't grant credits - should fail when trying to reserve

        var audioBytes = new byte[] { 1, 2, 3, 4, 5 };
        var command = new ProcessAudioNoteCommand
        {
            OwnerUserId = ownerUserId,
            ContactId = contactId,
            AudioBytes = audioBytes,
            FileName = "audio-note.mp3",
            MimeType = "audio/mpeg"
        };

        // Act & Assert
        var act = async () => await SendAsync(command);
        await act.Should().ThrowAsync<Domain.Exceptions.DomainException>();
    }
}

