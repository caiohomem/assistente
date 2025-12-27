using AssistenteExecutivo.Api.Auth;
using AssistenteExecutivo.Api.Extensions;
using AssistenteExecutivo.Application.Commands.Capture;
using AssistenteExecutivo.Application.Queries.Capture;
using AssistenteExecutivo.Infrastructure.Services;
using MediatR;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AssistenteExecutivo.Api.Controllers;

[ApiController]
[Route("api/capture")]
[Authorize(AuthenticationSchemes = $"{BffSessionAuthenticationDefaults.Scheme},{JwtBearerDefaults.AuthenticationScheme}")]
public sealed class CaptureController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly AudioTrimmer _audioTrimmer;

    public CaptureController(IMediator mediator, AudioTrimmer audioTrimmer)
    {
        _mediator = mediator;
        _audioTrimmer = audioTrimmer;
    }

    /// <summary>
    /// Upload de cartão de visita para processamento OCR
    /// </summary>
    [HttpPost("upload-card")]
    [Consumes("multipart/form-data")]
    public async Task<IActionResult> UploadCard(
        IFormFile file,
        CancellationToken cancellationToken)
    {
        if (file == null || file.Length == 0)
        {
            return BadRequest(new { message = "Arquivo é obrigatório." });
        }

        // Validar tipo de arquivo (imagem)
        var allowedMimeTypes = new[] { "image/jpeg", "image/jpg", "image/png", "image/webp" };
        if (!allowedMimeTypes.Contains(file.ContentType.ToLowerInvariant()))
        {
            return BadRequest(new { message = "Tipo de arquivo não suportado. Use JPEG, PNG ou WebP." });
        }

        // Validar tamanho (máximo 10MB)
        const long maxFileSize = 10 * 1024 * 1024; // 10MB
        if (file.Length > maxFileSize)
        {
            return BadRequest(new { message = "Arquivo muito grande. Tamanho máximo: 10MB." });
        }

        // Obter OwnerUserId
        var ownerUserId = await HttpContext.GetRequiredOwnerUserIdAsync(_mediator, cancellationToken);

        // Ler bytes do arquivo
        byte[] imageBytes;
        using (var memoryStream = new MemoryStream())
        {
            await file.CopyToAsync(memoryStream, cancellationToken);
            imageBytes = memoryStream.ToArray();
        }

        // Criar command
        var command = new UploadCardCommand
        {
            OwnerUserId = ownerUserId,
            ImageBytes = imageBytes,
            FileName = file.FileName,
            MimeType = file.ContentType
        };

        // Executar command
        var result = await _mediator.Send(command, cancellationToken);

        return Ok(new
        {
            contactId = result.ContactId,
            jobId = result.JobId,
            mediaId = result.MediaId,
            message = "Cartão enviado com sucesso. Processamento iniciado."
        });
    }

    /// <summary>
    /// Processar nota de áudio
    /// </summary>
    [HttpPost("audio-note")]
    [Consumes("multipart/form-data")]
    public async Task<IActionResult> ProcessAudioNote(
        [FromForm] Guid contactId,
        IFormFile file,
        CancellationToken cancellationToken)
    {
        if (contactId == Guid.Empty)
        {
            return BadRequest(new { message = "ContactId é obrigatório." });
        }

        if (file == null || file.Length == 0)
        {
            return BadRequest(new { message = "Arquivo de áudio é obrigatório." });
        }

        // Validar tipo de arquivo (áudio)
        // Formatos suportados pela API OpenAI Whisper: mp3, mp4, mpeg, mpga, m4a, wav, webm
        var contentType = file.ContentType ?? string.Empty;
        var allowedMimeTypes = new[] { 
            "audio/mpeg", "audio/mp3", "audio/mp4", "audio/mpeg", "audio/mpga", 
            "audio/m4a", "audio/wav", "audio/webm", "audio/ogg" 
        };
        if (string.IsNullOrWhiteSpace(contentType) || !allowedMimeTypes.Contains(contentType.ToLowerInvariant()))
        {
            return BadRequest(new { 
                message = "Tipo de arquivo não suportado. Use MP3, MP4, WAV, WebM, M4A ou OGG." 
            });
        }

        // Obter OwnerUserId
        Guid ownerUserId;
        try
        {
            ownerUserId = await HttpContext.GetRequiredOwnerUserIdAsync(_mediator, cancellationToken);
        }
        catch (UnauthorizedAccessException)
        {
            // Re-throw authentication exceptions - they're handled by middleware
            throw;
        }
        catch (Exception ex)
        {
            // Log unexpected errors during user ID retrieval
            throw new InvalidOperationException($"Erro ao obter ID do usuário: {ex.Message}", ex);
        }

        // Ler bytes do arquivo
        byte[] originalAudioBytes;
        try
        {
            using (var memoryStream = new MemoryStream())
            {
                await file.CopyToAsync(memoryStream, cancellationToken);
                originalAudioBytes = memoryStream.ToArray();
            }
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException($"Erro ao ler arquivo de áudio: {ex.Message}", ex);
        }

        // Validar que o arquivo não está vazio após leitura
        if (originalAudioBytes == null || originalAudioBytes.Length == 0)
        {
            return BadRequest(new { message = "Arquivo de áudio está vazio ou não pôde ser lido." });
        }

        // Cortar áudio automaticamente se exceder 25MB (limite da API OpenAI Whisper)
        byte[] audioBytes;
        bool wasTrimmed;
        string trimmedMessage;
        try
        {
            (audioBytes, wasTrimmed) = _audioTrimmer.TrimToMaxSize(originalAudioBytes, contentType);
            trimmedMessage = _audioTrimmer.GetTrimmedMessage(originalAudioBytes, wasTrimmed);
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException($"Erro ao processar arquivo de áudio: {ex.Message}", ex);
        }

        // Criar command
        var command = new ProcessAudioNoteCommand
        {
            OwnerUserId = ownerUserId,
            ContactId = contactId,
            AudioBytes = audioBytes,
            FileName = file.FileName ?? "audio-note",
            MimeType = contentType
        };

        // Executar command
        ProcessAudioNoteCommandResult result;
        try
        {
            result = await _mediator.Send(command, cancellationToken);
        }
        catch (Exception ex)
        {
            // Log the exception with context before rethrowing (middleware will handle it)
            // This helps with debugging when the exception details aren't in the main log
            throw;
        }

        var successMessage = string.IsNullOrWhiteSpace(trimmedMessage)
            ? "Nota de áudio processada com sucesso."
            : $"Nota de áudio processada com sucesso. {trimmedMessage}";

        return Ok(new
        {
            noteId = result.NoteId,
            jobId = result.JobId,
            mediaId = result.MediaId,
            status = result.Status,
            audioTranscript = result.AudioTranscript,
            audioSummary = result.AudioSummary,
            extractedTasks = result.ExtractedTasks,
            requestedAt = result.RequestedAt,
            completedAt = result.CompletedAt,
            errorCode = result.ErrorCode,
            errorMessage = result.ErrorMessage,
            responseMediaId = result.ResponseMediaId,
            message = successMessage,
            wasTrimmed = wasTrimmed
        });
    }

    /// <summary>
    /// Obter job de captura por ID
    /// </summary>
    [HttpGet("jobs/{id:guid}")]
    public async Task<IActionResult> GetJobById(
        [FromRoute] Guid id,
        CancellationToken cancellationToken)
    {
        if (id == Guid.Empty)
        {
            return BadRequest(new { message = "JobId é obrigatório." });
        }

        // Obter OwnerUserId
        var ownerUserId = await HttpContext.GetRequiredOwnerUserIdAsync(_mediator, cancellationToken);

        // Criar query
        var query = new GetCaptureJobByIdQuery
        {
            JobId = id,
            OwnerUserId = ownerUserId
        };

        // Executar query
        var job = await _mediator.Send(query, cancellationToken);

        if (job == null)
        {
            return NotFound(new { message = "Job não encontrado." });
        }

        return Ok(job);
    }

    /// <summary>
    /// Listar jobs de captura do usuário
    /// </summary>
    [HttpGet("jobs")]
    public async Task<IActionResult> ListJobs(
        CancellationToken cancellationToken)
    {
        // Obter OwnerUserId
        var ownerUserId = await HttpContext.GetRequiredOwnerUserIdAsync(_mediator, cancellationToken);

        // Criar query
        var query = new ListCaptureJobsQuery
        {
            OwnerUserId = ownerUserId
        };

        // Executar query
        var jobs = await _mediator.Send(query, cancellationToken);

        return Ok(jobs);
    }
}

