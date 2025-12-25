using AssistenteExecutivo.Domain.DomainEvents;
using AssistenteExecutivo.Domain.Entities;
using AssistenteExecutivo.Domain.Enums;
using AssistenteExecutivo.Domain.Exceptions;
using AssistenteExecutivo.Domain.Interfaces;
using AssistenteExecutivo.Domain.ValueObjects;
using FluentAssertions;

namespace AssistenteExecutivo.Domain.Tests.Entities;

public class CaptureJobTests
{
    private readonly IClock _clock;

    public CaptureJobTests()
    {
        _clock = new TestClock();
    }

    [Fact]
    public void RequestCardScan_ShouldCreateWithRequestedStatus()
    {
        // Arrange
        var jobId = Guid.NewGuid();
        var ownerUserId = Guid.NewGuid();
        var mediaId = Guid.NewGuid();

        // Act
        var job = CaptureJob.RequestCardScan(jobId, ownerUserId, mediaId, _clock);

        // Assert
        job.JobId.Should().Be(jobId);
        job.Type.Should().Be(JobType.CardScan);
        job.Status.Should().Be(JobStatus.Requested);
        job.DomainEvents.Should().ContainSingle()
            .Which.Should().BeOfType<CaptureJobRequested>();
    }

    [Fact]
    public void MarkProcessing_FromRequested_ShouldChangeStatus()
    {
        // Arrange
        var job = CreateCardScanJob();
        job.ClearDomainEvents();

        // Act
        job.MarkProcessing(_clock);

        // Assert
        job.Status.Should().Be(JobStatus.Processing);
    }

    [Fact]
    public void CompleteCardScan_ValidResult_ShouldComplete()
    {
        // Arrange
        var job = CreateCardScanJob();
        job.MarkProcessing(_clock);
        job.ClearDomainEvents();
        var extract = new OcrExtract(
            name: "João Silva",
            email: "joao@example.com",
            phone: "11987654321");

        // Act
        job.CompleteCardScan(extract, _clock);

        // Assert
        job.Status.Should().Be(JobStatus.Succeeded);
        job.CardScanResult.Should().NotBeNull();
        job.CompletedAt.Should().NotBeNull();
        job.DomainEvents.Should().ContainSingle()
            .Which.Should().BeOfType<CaptureJobCompleted>();
    }

    [Fact]
    public void CompleteCardScan_NotProcessing_ShouldThrow()
    {
        // Arrange
        var job = CreateCardScanJob();
        var extract = new OcrExtract();

        // Act & Assert
        var act = () => job.CompleteCardScan(extract, _clock);
        act.Should().Throw<DomainException>()
            .WithMessage("*JobApenasProcessingPodeSerCompletado*");
    }

    [Fact]
    public void Fail_ShouldChangeStatusToFailed()
    {
        // Arrange
        var job = CreateCardScanJob();
        job.MarkProcessing(_clock);
        job.ClearDomainEvents();

        // Act
        job.Fail("ERROR_001", "Erro ao processar", _clock);

        // Assert
        job.Status.Should().Be(JobStatus.Failed);
        job.ErrorCode.Should().Be("ERROR_001");
        job.ErrorMessage.Should().Be("Erro ao processar");
        job.DomainEvents.Should().ContainSingle()
            .Which.Should().BeOfType<CaptureJobFailed>();
    }

    [Fact]
    public void Fail_AlreadySucceeded_ShouldThrow()
    {
        // Arrange
        var job = CreateCardScanJob();
        job.MarkProcessing(_clock);
        job.CompleteCardScan(new OcrExtract(), _clock);

        // Act & Assert
        var act = () => job.Fail("ERROR", "Erro", _clock);
        act.Should().Throw<DomainException>()
            .WithMessage("*JobConcluidoNaoPodeFalhar*");
    }

    [Fact]
    public void Fail_AlreadyFailed_ShouldThrow()
    {
        // Arrange
        var job = CreateCardScanJob();
        job.MarkProcessing(_clock);
        job.Fail("ERROR_001", "Erro", _clock);

        // Act & Assert
        var act = () => job.Fail("ERROR_002", "Erro 2", _clock);
        act.Should().Throw<DomainException>()
            .WithMessage("*JobConcluidoNaoPodeFalhar*");
    }

    [Fact]
    public void RequestAudioProcessing_ShouldCreateWithRequestedStatus()
    {
        // Arrange
        var jobId = Guid.NewGuid();
        var ownerUserId = Guid.NewGuid();
        var contactId = Guid.NewGuid();
        var mediaId = Guid.NewGuid();

        // Act
        var job = CaptureJob.RequestAudioProcessing(jobId, ownerUserId, contactId, mediaId, _clock);

        // Assert
        job.JobId.Should().Be(jobId);
        job.Type.Should().Be(JobType.AudioNoteTranscription);
        job.Status.Should().Be(JobStatus.Requested);
        job.ContactId.Should().Be(contactId);
        job.DomainEvents.Should().ContainSingle()
            .Which.Should().BeOfType<CaptureJobRequested>();
    }

    [Fact]
    public void CompleteAudioProcessing_ValidData_ShouldComplete()
    {
        // Arrange
        var jobId = Guid.NewGuid();
        var ownerUserId = Guid.NewGuid();
        var contactId = Guid.NewGuid();
        var mediaId = Guid.NewGuid();
        var job = CaptureJob.RequestAudioProcessing(jobId, ownerUserId, contactId, mediaId, _clock);
        job.MarkProcessing(_clock);
        job.ClearDomainEvents();

        var transcript = new Transcript("Transcrição completa");
        var summary = "Resumo do áudio";
        var tasks = new List<ExtractedTask> { new ExtractedTask("Tarefa 1") };

        // Act
        job.CompleteAudioProcessing(transcript, summary, tasks, _clock);

        // Assert
        job.Status.Should().Be(JobStatus.Succeeded);
        job.AudioTranscript.Should().NotBeNull();
        job.AudioSummary.Should().Be(summary);
        job.ExtractedTasks.Should().HaveCount(1);
        job.CompletedAt.Should().NotBeNull();
        job.DomainEvents.Should().ContainSingle()
            .Which.Should().BeOfType<CaptureJobCompleted>();
    }

    [Fact]
    public void CompleteAudioProcessing_NotProcessing_ShouldThrow()
    {
        // Arrange
        var jobId = Guid.NewGuid();
        var ownerUserId = Guid.NewGuid();
        var contactId = Guid.NewGuid();
        var mediaId = Guid.NewGuid();
        var job = CaptureJob.RequestAudioProcessing(jobId, ownerUserId, contactId, mediaId, _clock);
        // Job is in Requested status, not Processing
        var transcript = new Transcript("Texto");
        var summary = "Resumo";

        // Act & Assert
        var act = () => job.CompleteAudioProcessing(transcript, summary, null, _clock);
        act.Should().Throw<DomainException>()
            .WithMessage("*JobApenasProcessingPodeSerCompletado*");
    }

    [Fact]
    public void CompleteAudioProcessing_WrongJobType_ShouldThrow()
    {
        // Arrange
        var job = CreateCardScanJob();
        job.MarkProcessing(_clock);
        var transcript = new Transcript("Texto");
        var summary = "Resumo";

        // Act & Assert
        var act = () => job.CompleteAudioProcessing(transcript, summary, null, _clock);
        act.Should().Throw<DomainException>()
            .WithMessage("*JobTipoInvalido*");
    }

    [Fact]
    public void CompleteAudioProcessing_NullTranscript_ShouldThrow()
    {
        // Arrange
        var jobId = Guid.NewGuid();
        var ownerUserId = Guid.NewGuid();
        var contactId = Guid.NewGuid();
        var mediaId = Guid.NewGuid();
        var job = CaptureJob.RequestAudioProcessing(jobId, ownerUserId, contactId, mediaId, _clock);
        job.MarkProcessing(_clock);

        // Act & Assert
        var act = () => job.CompleteAudioProcessing(null!, "Summary", null, _clock);
        act.Should().Throw<DomainException>()
            .WithMessage("*TranscriptObrigatorio*");
    }

    [Fact]
    public void CompleteAudioProcessing_EmptySummary_ShouldThrow()
    {
        // Arrange
        var jobId = Guid.NewGuid();
        var ownerUserId = Guid.NewGuid();
        var contactId = Guid.NewGuid();
        var mediaId = Guid.NewGuid();
        var job = CaptureJob.RequestAudioProcessing(jobId, ownerUserId, contactId, mediaId, _clock);
        job.MarkProcessing(_clock);
        var transcript = new Transcript("Texto");

        // Act & Assert
        var act = () => job.CompleteAudioProcessing(transcript, "", null, _clock);
        act.Should().Throw<DomainException>()
            .WithMessage("*SummaryObrigatorio*");
    }

    [Fact]
    public void CompleteCardScan_NullExtract_ShouldThrow()
    {
        // Arrange
        var job = CreateCardScanJob();
        job.MarkProcessing(_clock);

        // Act & Assert
        var act = () => job.CompleteCardScan(null!, _clock);
        act.Should().Throw<DomainException>()
            .WithMessage("*OcrExtractObrigatorio*");
    }

    [Fact]
    public void CompleteCardScan_WrongJobType_ShouldThrow()
    {
        // Arrange
        var jobId = Guid.NewGuid();
        var ownerUserId = Guid.NewGuid();
        var contactId = Guid.NewGuid();
        var mediaId = Guid.NewGuid();
        var job = CaptureJob.RequestAudioProcessing(jobId, ownerUserId, contactId, mediaId, _clock);
        job.MarkProcessing(_clock);
        var extract = new OcrExtract();

        // Act & Assert
        var act = () => job.CompleteCardScan(extract, _clock);
        act.Should().Throw<DomainException>()
            .WithMessage("*JobTipoInvalido*");
    }

    [Fact]
    public void MarkProcessing_NotRequested_ShouldThrow()
    {
        // Arrange
        var job = CreateCardScanJob();
        job.MarkProcessing(_clock);

        // Act & Assert
        var act = () => job.MarkProcessing(_clock);
        act.Should().Throw<DomainException>()
            .WithMessage("*JobApenasRequestedPodeSerProcessado*");
    }

    private CaptureJob CreateCardScanJob()
    {
        var jobId = Guid.NewGuid();
        var ownerUserId = Guid.NewGuid();
        var mediaId = Guid.NewGuid();
        return CaptureJob.RequestCardScan(jobId, ownerUserId, mediaId, _clock);
    }

    private class TestClock : IClock
    {
        public DateTime UtcNow => DateTime.UtcNow;
    }
}

