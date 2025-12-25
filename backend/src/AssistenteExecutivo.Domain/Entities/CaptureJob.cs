using AssistenteExecutivo.Domain.DomainEvents;
using AssistenteExecutivo.Domain.Enums;
using AssistenteExecutivo.Domain.Exceptions;
using AssistenteExecutivo.Domain.Interfaces;
using AssistenteExecutivo.Domain.ValueObjects;

namespace AssistenteExecutivo.Domain.Entities;

public class CaptureJob
{
    private readonly List<IDomainEvent> _domainEvents = new();

    private CaptureJob() { } // EF Core

    public CaptureJob(
        Guid jobId,
        Guid ownerUserId,
        JobType type,
        Guid mediaId,
        Guid? contactId,
        IClock clock)
    {
        if (jobId == Guid.Empty)
            throw new DomainException("Domain:JobIdObrigatorio");

        if (ownerUserId == Guid.Empty)
            throw new DomainException("Domain:OwnerUserIdObrigatorio");

        if (mediaId == Guid.Empty)
            throw new DomainException("Domain:MediaIdObrigatorio");

        if (clock == null)
            throw new DomainException("Domain:ClockObrigatorio");

        JobId = jobId;
        OwnerUserId = ownerUserId;
        Type = type;
        MediaId = mediaId;
        ContactId = contactId;
        Status = JobStatus.Requested;
        RequestedAt = clock.UtcNow;
    }

    public Guid JobId { get; private set; }
    public Guid OwnerUserId { get; private set; }
    public JobType Type { get; private set; }
    public Guid? ContactId { get; private set; }
    public Guid MediaId { get; private set; }
    public JobStatus Status { get; private set; }
    public DateTime RequestedAt { get; private set; }
    public DateTime? CompletedAt { get; private set; }
    public string? ErrorCode { get; private set; }
    public string? ErrorMessage { get; private set; }

    // Resultados específicos por tipo
    public OcrExtract? CardScanResult { get; private set; }
    public Transcript? AudioTranscript { get; private set; }
    public string? AudioSummary { get; private set; }
    public List<ExtractedTask>? ExtractedTasks { get; private set; }

    public IReadOnlyCollection<IDomainEvent> DomainEvents => _domainEvents.AsReadOnly();

    public static CaptureJob RequestCardScan(
        Guid jobId,
        Guid ownerUserId,
        Guid mediaId,
        IClock clock)
    {
        var job = new CaptureJob(jobId, ownerUserId, JobType.CardScan, mediaId, null, clock);
        job._domainEvents.Add(new CaptureJobRequested(jobId, JobType.CardScan, clock.UtcNow));
        return job;
    }

    public static CaptureJob RequestAudioProcessing(
        Guid jobId,
        Guid ownerUserId,
        Guid contactId,
        Guid mediaId,
        IClock clock)
    {
        var job = new CaptureJob(jobId, ownerUserId, JobType.AudioNoteTranscription, mediaId, contactId, clock);
        job._domainEvents.Add(new CaptureJobRequested(jobId, JobType.AudioNoteTranscription, clock.UtcNow));
        return job;
    }

    public void MarkProcessing(IClock clock)
    {
        if (Status != JobStatus.Requested)
            throw new DomainException("Domain:JobApenasRequestedPodeSerProcessado");

        Status = JobStatus.Processing;
    }

    public void CompleteCardScan(OcrExtract result, IClock clock)
    {
        if (Type != JobType.CardScan)
            throw new DomainException("Domain:JobTipoInvalido");

        if (Status != JobStatus.Processing)
            throw new DomainException("Domain:JobApenasProcessingPodeSerCompletado");

        if (result == null)
            throw new DomainException("Domain:OcrExtractObrigatorio");

        CardScanResult = result;
        Status = JobStatus.Succeeded;
        CompletedAt = clock.UtcNow;

        _domainEvents.Add(new CaptureJobCompleted(JobId, Type, clock.UtcNow));
    }

    public void CompleteAudioProcessing(
        Transcript transcript,
        string summary,
        List<ExtractedTask> tasks,
        IClock clock)
    {
        if (Type != JobType.AudioNoteTranscription)
            throw new DomainException("Domain:JobTipoInvalido");

        if (Status != JobStatus.Processing)
            throw new DomainException("Domain:JobApenasProcessingPodeSerCompletado");

        if (transcript == null)
            throw new DomainException("Domain:TranscriptObrigatorio");

        if (string.IsNullOrWhiteSpace(summary))
            throw new DomainException("Domain:SummaryObrigatorio");

        AudioTranscript = transcript;
        AudioSummary = summary;
        ExtractedTasks = tasks ?? new List<ExtractedTask>();
        Status = JobStatus.Succeeded;
        CompletedAt = clock.UtcNow;

        _domainEvents.Add(new CaptureJobCompleted(JobId, Type, clock.UtcNow));
    }

    public void Fail(string errorCode, string errorMessage, IClock clock)
    {
        if (Status == JobStatus.Succeeded || Status == JobStatus.Failed)
            throw new DomainException("Domain:JobConcluidoNaoPodeFalhar");

        Status = JobStatus.Failed;
        ErrorCode = errorCode;
        ErrorMessage = errorMessage;
        CompletedAt = clock.UtcNow;

        _domainEvents.Add(new CaptureJobFailed(JobId, errorCode, clock.UtcNow));
    }

    public void ClearDomainEvents()
    {
        _domainEvents.Clear();
    }
}

