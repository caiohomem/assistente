using AssistenteExecutivo.Application.Commands.Capture;
using AssistenteExecutivo.Application.DTOs;
using AssistenteExecutivo.Application.Interfaces;
using AssistenteExecutivo.Application.Mappings;
using AssistenteExecutivo.Domain.Entities;
using AssistenteExecutivo.Domain.Enums;
using AssistenteExecutivo.Domain.Interfaces;
using AssistenteExecutivo.Domain.ValueObjects;
using MediatR;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
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
    private readonly ITextToSpeechProvider? _textToSpeechProvider;
    private readonly IFileStore _fileStore;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IClock _clock;
    private readonly IIdGenerator _idGenerator;
    private readonly IConfiguration _configuration;
    private readonly ILogger<ProcessAudioNoteCommandHandler> _logger;

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
        ITextToSpeechProvider? textToSpeechProvider,
        IFileStore fileStore,
        IUnitOfWork unitOfWork,
        IClock clock,
        IIdGenerator idGenerator,
        IConfiguration configuration,
        ILogger<ProcessAudioNoteCommandHandler> logger)
    {
        _mediaAssetRepository = mediaAssetRepository;
        _captureJobRepository = captureJobRepository;
        _contactRepository = contactRepository;
        _noteRepository = noteRepository;
        _creditWalletRepository = creditWalletRepository;
        _speechToTextProvider = speechToTextProvider;
        _llmProvider = llmProvider;
        _textToSpeechProvider = textToSpeechProvider;
        _fileStore = fileStore;
        _unitOfWork = unitOfWork;
        _clock = clock;
        _idGenerator = idGenerator;
        _configuration = configuration;
        _logger = logger;
    }

    public async Task<ProcessAudioNoteCommandResult> Handle(ProcessAudioNoteCommand request, CancellationToken cancellationToken)
    {
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
            await _creditWalletRepository.UpdateAsync(wallet, cancellationToken);
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
            await _creditWalletRepository.UpdateAsync(wallet, cancellationToken);
            // Save changes before throwing
            await _unitOfWork.SaveChangesAsync(cancellationToken);
            throw;
        }

        // Validar que o summary foi gerado corretamente
        if (string.IsNullOrWhiteSpace(llmResult.Summary))
        {
            throw new InvalidOperationException("OpenAI LLM retornou summary vazio. Verifique a resposta da API.");
        }
        
        var summary = llmResult.Summary;

        // 6. Generate TTS response audio (if enabled and provider available)
        Guid? responseMediaId = null;
        var ttsEnabledValue = _configuration["OpenAI:TextToSpeech:Enabled"];
        var ttsEnabled = !string.IsNullOrWhiteSpace(ttsEnabledValue) && 
                         bool.TryParse(ttsEnabledValue, out var enabled) && enabled;
        if (ttsEnabled && _textToSpeechProvider != null && !string.IsNullOrWhiteSpace(summary))
        {
            try
            {
                var ttsVoice = _configuration["OpenAI:TextToSpeech:Voice"] ?? "nova";
                var ttsFormat = _configuration["OpenAI:TextToSpeech:Format"] ?? "mp3";
                
                _logger.LogInformation("Gerando áudio de resposta via TTS. Summary length: {Length}, Voice: {Voice}", 
                    summary.Length, ttsVoice);
                
                var audioBytes = await _textToSpeechProvider.SynthesizeAsync(
                    summary,
                    ttsVoice,
                    ttsFormat,
                    cancellationToken);
                
                if (audioBytes != null && audioBytes.Length > 0)
                {
                    // Criar MediaAsset para o áudio gerado
                    var responseHash = await _fileStore.ComputeHashAsync(audioBytes, cancellationToken);
                    responseMediaId = _idGenerator.NewGuid();
                    var responseStorageKey = $"db/{responseMediaId}";
                    var responseMimeType = ttsFormat switch
                    {
                        "mp3" => "audio/mpeg",
                        "opus" => "audio/opus",
                        "aac" => "audio/aac",
                        "flac" => "audio/flac",
                        _ => "audio/mpeg"
                    };
                    var responseMediaRef = MediaRef.Create(responseStorageKey, responseHash, responseMimeType, audioBytes.Length);
                    var responseMediaAsset = new MediaAsset(responseMediaId.Value, request.OwnerUserId, responseMediaRef, MediaKind.Audio, _clock);
                    responseMediaAsset.SetFileContent(audioBytes);
                    await _mediaAssetRepository.AddAsync(responseMediaAsset, cancellationToken);
                    
                    _logger.LogInformation("Áudio de resposta gerado e armazenado. MediaId: {MediaId}, Size: {Size} bytes", 
                        responseMediaId, audioBytes.Length);
                }
            }
            catch (Exception ex)
            {
                // Log erro mas não falhar o processamento principal
                _logger.LogWarning(ex, "Erro ao gerar áudio de resposta via TTS. Continuando sem áudio de resposta.");
            }
        }

        // 7. Complete the capture job
        // Campo Description agora é ilimitado (nvarchar(max)/text), não precisa truncar
        captureJob.CompleteAudioProcessing(transcript, summary, llmResult.Tasks, _clock);
        // Note: No need to call UpdateAsync here - captureJob is tracked and EF Core will detect changes

        // 8. Create Note
        // Validar que temos conteúdo para a nota
        if (string.IsNullOrWhiteSpace(transcript.Text))
        {
            throw new InvalidOperationException("Transcrição vazia. Não é possível criar nota sem conteúdo.");
        }
        
        var rawContent = transcript.Text;

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
            }).ToList(),
            responseMediaId = responseMediaId
        };
        var structuredDataJson = System.Text.Json.JsonSerializer.Serialize(structuredData);
        note.UpdateStructuredData(structuredDataJson, _clock);
        
        await _noteRepository.AddAsync(note, cancellationToken);

        // 9. Consume credits
        wallet.Consume(creditAmount, consumeIdempotencyKey, $"Audio note processing completed for contact {request.ContactId}", _clock);
        
        await _creditWalletRepository.UpdateAsync(wallet, cancellationToken);

        // 10. Save all changes to database
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        // 11. Map job to DTO for response
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
            ErrorMessage = jobDto.ErrorMessage,
            ResponseMediaId = responseMediaId
        };
    }
}


