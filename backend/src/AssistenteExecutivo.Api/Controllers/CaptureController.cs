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
        var allowedMimeTypes = new[] { 
            "audio/mpeg", "audio/mp3", "audio/mp4", "audio/mpeg", "audio/mpga", 
            "audio/m4a", "audio/wav", "audio/webm", "audio/ogg" 
        };
        if (!allowedMimeTypes.Contains(file.ContentType.ToLowerInvariant()))
        {
            return BadRequest(new { 
                message = "Tipo de arquivo não suportado. Use MP3, MP4, WAV, WebM, M4A ou OGG." 
            });
        }

        // Obter OwnerUserId
        var ownerUserId = await HttpContext.GetRequiredOwnerUserIdAsync(_mediator, cancellationToken);

        // Ler bytes do arquivo
        byte[] originalAudioBytes;
        using (var memoryStream = new MemoryStream())
        {
            await file.CopyToAsync(memoryStream, cancellationToken);
            originalAudioBytes = memoryStream.ToArray();
        }

        // Cortar áudio automaticamente se exceder 25MB (limite da API OpenAI Whisper)
        var (audioBytes, wasTrimmed) = _audioTrimmer.TrimToMaxSize(originalAudioBytes, file.ContentType);
        var trimmedMessage = _audioTrimmer.GetTrimmedMessage(originalAudioBytes, wasTrimmed);

        // Criar command
        var command = new ProcessAudioNoteCommand
        {
            OwnerUserId = ownerUserId,
            ContactId = contactId,
            AudioBytes = audioBytes,
            FileName = file.FileName,
            MimeType = file.ContentType
        };

        // Executar command
        var result = await _mediator.Send(command, cancellationToken);

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

