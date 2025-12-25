using AssistenteExecutivo.Application.Commands.Capture;
using AssistenteExecutivo.Application.DTOs;
using AssistenteExecutivo.Application.Interfaces;
using AssistenteExecutivo.Application.Mappings;
using AssistenteExecutivo.Domain.Entities;
using AssistenteExecutivo.Domain.Enums;
using AssistenteExecutivo.Domain.Interfaces;
using AssistenteExecutivo.Domain.ValueObjects;
using MediatR;
using System.Text.Json;

namespace AssistenteExecutivo.Application.Handlers.Capture;

public class ProcessAudioNoteCommandHandler : IRequestHandler<ProcessAudioNoteCommand, ProcessAudioNoteCommandResult>
{
    private readonly IMediaAssetRepository _mediaAssetRepository;
    private readonly ICaptureJobRepository _captureJobRepository;
    private readonly IContactRepository _contactRepository;
    private readonly INoteRepository _noteRepository;
    private readonly ICreditWalletRepository _creditWalletRepository;
    private readonly ISpeechToTextProvider _speechToTextProvider;
    private readonly ILLMProvider _llmProvider;
    private readonly IFileStore _fileStore;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IClock _clock;
    private readonly IIdGenerator _idGenerator;

    // Credit costs (can be moved to configuration)
    private const decimal AudioProcessingCreditCost = 1.0m;

    public ProcessAudioNoteCommandHandler(
        IMediaAssetRepository mediaAssetRepository,
        ICaptureJobRepository captureJobRepository,
        IContactRepository contactRepository,
        INoteRepository noteRepository,
        ICreditWalletRepository creditWalletRepository,
        ISpeechToTextProvider speechToTextProvider,
        ILLMProvider llmProvider,
        IFileStore fileStore,
        IUnitOfWork unitOfWork,
        IClock clock,
        IIdGenerator idGenerator)
    {
        _mediaAssetRepository = mediaAssetRepository;
        _captureJobRepository = captureJobRepository;
        _contactRepository = contactRepository;
        _noteRepository = noteRepository;
        _creditWalletRepository = creditWalletRepository;
        _speechToTextProvider = speechToTextProvider;
        _llmProvider = llmProvider;
        _fileStore = fileStore;
        _unitOfWork = unitOfWork;
        _clock = clock;
        _idGenerator = idGenerator;
    }

    public async Task<ProcessAudioNoteCommandResult> Handle(ProcessAudioNoteCommand request, CancellationToken cancellationToken)
    {
        // #region agent log
        try { System.IO.File.AppendAllText(@"c:\Projects\AssistenteExecutivo\.cursor\debug.log", JsonSerializer.Serialize(new { sessionId = "debug-session", runId = "run1", hypothesisId = "A,B,C,D,E", location = "ProcessAudioNoteCommandHandler.cs:56", message = "Handler started", data = new { contactId = request.ContactId.ToString(), ownerUserId = request.OwnerUserId.ToString() }, timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() }) + "\n"); } catch { }
        // #endregion

        // Validate contact exists
        var contact = await _contactRepository.GetByIdAsync(request.ContactId, request.OwnerUserId, cancellationToken);
        if (contact == null)
        {
            throw new InvalidOperationException($"Contact {request.ContactId} not found for user {request.OwnerUserId}");
        }

        // 1. Store the file and create MediaAsset
        // Usar MediaId como storageKey para referência (mesmo que o arquivo esteja no banco)
        var hash = await _fileStore.ComputeHashAsync(request.AudioBytes, cancellationToken);
        var mediaId = _idGenerator.NewGuid();
        var storageKey = $"db/{mediaId}"; // Prefixo "db/" indica que está no banco de dados
        var mediaRef = MediaRef.Create(storageKey, hash, request.MimeType, request.AudioBytes.Length);

        var mediaAsset = new MediaAsset(mediaId, request.OwnerUserId, mediaRef, MediaKind.Audio, _clock);
        // Armazenar o conteúdo do arquivo diretamente no banco de dados
        mediaAsset.SetFileContent(request.AudioBytes);
        await _mediaAssetRepository.AddAsync(mediaAsset, cancellationToken);

        // 2. Create CaptureJob
        var jobId = _idGenerator.NewGuid();
        var captureJob = CaptureJob.RequestAudioProcessing(jobId, request.OwnerUserId, request.ContactId, mediaId, _clock);
        await _captureJobRepository.AddAsync(captureJob, cancellationToken);

        // 3. Reserve credits
        var creditAmount = CreditAmount.Create(AudioProcessingCreditCost);
        var operationIdempotencyKey = IdempotencyKey.Generate();
        var reserveIdempotencyKey = IdempotencyKey.Create($"{operationIdempotencyKey.Value}-reserve");
        var consumeIdempotencyKey = IdempotencyKey.Create($"{operationIdempotencyKey.Value}-consume");
        var refundIdempotencyKey = IdempotencyKey.Create($"{operationIdempotencyKey.Value}-refund");
        var wallet = await _creditWalletRepository.GetOrCreateAsync(request.OwnerUserId, cancellationToken);
        wallet.Reserve(creditAmount, reserveIdempotencyKey, $"Audio note processing for contact {request.ContactId}", _clock);
        // Note: No need to call UpdateAsync here - wallet is tracked and EF Core will detect new transactions

        // 4. Process audio with Speech-to-Text
        captureJob.MarkProcessing(_clock);
        // Note: No need to call UpdateAsync here - captureJob is tracked and EF Core will detect changes

        Transcript transcript;
        try
        {
            transcript = await _speechToTextProvider.TranscribeAsync(request.AudioBytes, request.MimeType, cancellationToken);
        }
        catch (Exception ex)
        {
            captureJob.Fail("SPEECH_TO_TEXT_ERROR", ex.Message, _clock);
            // Refund reserved credits on failure
            wallet.Refund(creditAmount, refundIdempotencyKey, $"Refund for failed audio processing: {ex.Message}", _clock);
            // Save changes before throwing
            await _unitOfWork.SaveChangesAsync(cancellationToken);
            throw;
        }

        // 5. Process transcript with LLM
        AudioProcessingResult llmResult;
        try
        {
            llmResult = await _llmProvider.SummarizeAndExtractTasksAsync(transcript.Text, cancellationToken);
        }
        catch (Exception ex)
        {
            captureJob.Fail("LLM_PROCESSING_ERROR", ex.Message, _clock);
            // Refund reserved credits on failure
            wallet.Refund(creditAmount, refundIdempotencyKey, $"Refund for failed LLM processing: {ex.Message}", _clock);
            // Save changes before throwing
            await _unitOfWork.SaveChangesAsync(cancellationToken);
            throw;
        }

        // Ensure summary is not empty (fallback to transcript if needed)
        var summary = string.IsNullOrWhiteSpace(llmResult.Summary)
            ? (string.IsNullOrWhiteSpace(transcript.Text)
                ? "Nota de áudio processada."
                : transcript.Text.Length > 200
                    ? transcript.Text.Substring(0, 200) + "..."
                    : transcript.Text)
            : llmResult.Summary;

        // 6. Complete the capture job
        captureJob.CompleteAudioProcessing(transcript, summary, llmResult.Tasks, _clock);
        // Note: No need to call UpdateAsync here - captureJob is tracked and EF Core will detect changes

        // 7. Create Note
        // Ensure rawContent is not empty (fallback to summary if transcript is empty)
        var rawContent = string.IsNullOrWhiteSpace(transcript.Text)
            ? summary
            : transcript.Text;

        var noteId = _idGenerator.NewGuid();
        var note = Note.CreateAudioNote(noteId, request.ContactId, request.OwnerUserId, rawContent, _clock);
        
        // Store structured data (summary and tasks) as JSON
        var structuredData = new
        {
            summary = summary,
            tasks = llmResult.Tasks.Select(t => new
            {
                description = t.Description,
                dueDate = t.DueDate,
                priority = t.Priority
            }).ToList()
        };
        var structuredDataJson = System.Text.Json.JsonSerializer.Serialize(structuredData);
        note.UpdateStructuredData(structuredDataJson, _clock);
        
        await _noteRepository.AddAsync(note, cancellationToken);

        // 8. Consume credits
        // #region agent log
        try { System.IO.File.AppendAllText(@"c:\Projects\AssistenteExecutivo\.cursor\debug.log", JsonSerializer.Serialize(new { sessionId = "debug-session", runId = "run1", hypothesisId = "A,E", location = "ProcessAudioNoteCommandHandler.cs:168", message = "Before Consume", data = new { walletOwnerUserId = wallet.OwnerUserId.ToString(), transactionCount = wallet.Transactions.Count }, timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() }) + "\n"); } catch { }
        // #endregion
        
        wallet.Consume(creditAmount, consumeIdempotencyKey, $"Audio note processing completed for contact {request.ContactId}", _clock);
        
        // #region agent log
        try { System.IO.File.AppendAllText(@"c:\Projects\AssistenteExecutivo\.cursor\debug.log", JsonSerializer.Serialize(new { sessionId = "debug-session", runId = "run1", hypothesisId = "A,E", location = "ProcessAudioNoteCommandHandler.cs:173", message = "After Consume", data = new { walletOwnerUserId = wallet.OwnerUserId.ToString(), transactionCount = wallet.Transactions.Count, lastTransactionId = wallet.Transactions.LastOrDefault()?.TransactionId.ToString() }, timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() }) + "\n"); } catch { }
        // #endregion
        
        await _creditWalletRepository.UpdateAsync(wallet, cancellationToken);

        // 9. Save all changes to database
        // #region agent log
        try { System.IO.File.AppendAllText(@"c:\Projects\AssistenteExecutivo\.cursor\debug.log", JsonSerializer.Serialize(new { sessionId = "debug-session", runId = "run1", hypothesisId = "A,B,C,D,E", location = "ProcessAudioNoteCommandHandler.cs:180", message = "Before SaveChangesAsync", data = new { }, timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() }) + "\n"); } catch { }
        // #endregion
        
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        
        // #region agent log
        try { System.IO.File.AppendAllText(@"c:\Projects\AssistenteExecutivo\.cursor\debug.log", JsonSerializer.Serialize(new { sessionId = "debug-session", runId = "run1", hypothesisId = "A,B,C,D,E", location = "ProcessAudioNoteCommandHandler.cs:185", message = "SaveChangesAsync succeeded", data = new { }, timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() }) + "\n"); } catch { }
        // #endregion

        // 10. Map job to DTO for response
        var jobDto = CaptureJobMapper.MapToDto(captureJob);

        return new ProcessAudioNoteCommandResult
        {
            NoteId = noteId,
            JobId = jobId,
            MediaId = mediaId,
            Status = jobDto.Status.ToString(),
            AudioTranscript = jobDto.AudioTranscript,
            AudioSummary = jobDto.AudioSummary,
            ExtractedTasks = jobDto.ExtractedTasks,
            RequestedAt = jobDto.RequestedAt,
            CompletedAt = jobDto.CompletedAt,
            ErrorCode = jobDto.ErrorCode,
            ErrorMessage = jobDto.ErrorMessage
        };
    }
}


