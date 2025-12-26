using AssistenteExecutivo.Application.Commands.Capture;
using AssistenteExecutivo.Application.Commands.Contacts;
using AssistenteExecutivo.Application.Queries.Notes;
using AssistenteExecutivo.Application.Tests.Helpers;
using AssistenteExecutivo.Domain.Enums;
using AssistenteExecutivo.Domain.Interfaces;
using AssistenteExecutivo.Infrastructure.Persistence;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

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

public class ProcessAudioNoteCommandHandlerFailurePersistenceTests : HandlerTestBase
{
    protected override void ConfigureServices(IServiceCollection services)
    {
        services.AddScoped<ISpeechToTextProvider, ThrowingSpeechToTextProvider>();
    }

    [Fact]
    public async Task Handle_SpeechToTextFailure_ShouldPersistFailedJobAndRefund_ThenRethrow()
    {
        var ownerUserId = Guid.NewGuid();
        var contactId = await SendAsync(new CreateContactCommand
        {
            OwnerUserId = ownerUserId,
            FirstName = "João"
        });

        await SendAsync(new Application.Commands.Credits.GrantCreditsCommand
        {
            OwnerUserId = ownerUserId,
            Amount = 10m,
            Reason = "Test credits"
        });

        var command = new ProcessAudioNoteCommand
        {
            OwnerUserId = ownerUserId,
            ContactId = contactId,
            AudioBytes = new byte[] { 1, 2, 3 },
            FileName = "audio-note.mp3",
            MimeType = "audio/mpeg"
        };

        var act = async () => await SendAsync(command);
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*speech-to-text*");

        using var scope = ServiceProvider.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        var jobs = await db.CaptureJobs.Where(j => j.OwnerUserId == ownerUserId).ToListAsync();
        jobs.Should().HaveCount(1);
        jobs[0].Status.Should().Be(JobStatus.Failed);
        jobs[0].ErrorCode.Should().Be("SPEECH_TO_TEXT_ERROR");

        var notes = await db.Notes.ToListAsync();
        notes.Should().BeEmpty();

        var mediaAssets = await db.MediaAssets.Where(m => m.OwnerUserId == ownerUserId).ToListAsync();
        mediaAssets.Should().HaveCount(1);

        var wallet = await db.CreditWallets.FirstOrDefaultAsync(w => w.OwnerUserId == ownerUserId);
        wallet.Should().NotBeNull();

        var transactions = await db.CreditTransactions.Where(t => t.OwnerUserId == ownerUserId).ToListAsync();
        transactions.Select(t => t.Type).Should().Contain(CreditTransactionType.Reserve);
        transactions.Select(t => t.Type).Should().Contain(CreditTransactionType.Refund);
    }

    private sealed class ThrowingSpeechToTextProvider : ISpeechToTextProvider
    {
        public Task<Domain.ValueObjects.Transcript> TranscribeAsync(byte[] audioBytes, string mimeType, CancellationToken cancellationToken = default)
            => throw new InvalidOperationException("speech-to-text failed");
    }
}
