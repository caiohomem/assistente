using AssistenteExecutivo.Api.Auth;
using AssistenteExecutivo.Api.Extensions;
using AssistenteExecutivo.Application.Commands.Notes;
using AssistenteExecutivo.Application.DTOs;
using AssistenteExecutivo.Application.Interfaces;
using AssistenteExecutivo.Application.Queries.Notes;
using AssistenteExecutivo.Domain.Enums;
using AssistenteExecutivo.Domain.Interfaces;
using MediatR;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AssistenteExecutivo.Api.Controllers;

[ApiController]
[Route("api")]
[Authorize(AuthenticationSchemes = $"{BffSessionAuthenticationDefaults.Scheme},{JwtBearerDefaults.AuthenticationScheme}")]
public sealed class NotesController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly ILogger<NotesController> _logger;
    private readonly INoteRepository _noteRepository;
    private readonly ICaptureJobRepository _captureJobRepository;
    private readonly IMediaAssetRepository _mediaAssetRepository;
    private readonly IFileStore _fileStore;

    public NotesController(
        IMediator mediator,
        ILogger<NotesController> logger,
        INoteRepository noteRepository,
        ICaptureJobRepository captureJobRepository,
        IMediaAssetRepository mediaAssetRepository,
        IFileStore fileStore)
    {
        _mediator = mediator;
        _logger = logger;
        _noteRepository = noteRepository;
        _captureJobRepository = captureJobRepository;
        _mediaAssetRepository = mediaAssetRepository;
        _fileStore = fileStore;
    }

    /// <summary>
    /// Lista todas as notas de um contato específico.
    /// </summary>
    [HttpGet("contacts/{contactId}/notes")]
    [ProducesResponseType(typeof(List<NoteDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<List<NoteDto>>> ListNotesByContact(
        Guid contactId,
        CancellationToken cancellationToken)
    {
        var ownerUserId = await HttpContext.GetRequiredOwnerUserIdAsync(_mediator, cancellationToken);

        var query = new ListNotesByContactQuery
        {
            ContactId = contactId,
            OwnerUserId = ownerUserId
        };

        var notes = await _mediator.Send(query, cancellationToken);
        return Ok(notes);
    }

    /// <summary>
    /// Obtém uma nota específica por ID.
    /// </summary>
    [HttpGet("notes/{id}")]
    [ProducesResponseType(typeof(NoteDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<NoteDto>> GetNoteById(
        Guid id,
        CancellationToken cancellationToken)
    {
        var ownerUserId = await HttpContext.GetRequiredOwnerUserIdAsync(_mediator, cancellationToken);

        var query = new GetNoteByIdQuery
        {
            NoteId = id,
            OwnerUserId = ownerUserId
        };

        var note = await _mediator.Send(query, cancellationToken);
        if (note == null)
            return NotFound(new { message = "Nota não encontrada." });

        return Ok(note);
    }

    /// <summary>
    /// Cria uma nova nota de texto para um contato.
    /// </summary>
    [HttpPost("contacts/{contactId}/notes")]
    [ProducesResponseType(typeof(Guid), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<Guid>> CreateTextNote(
        Guid contactId,
        [FromBody] CreateTextNoteRequest request,
        CancellationToken cancellationToken)
    {
        if (request == null || string.IsNullOrWhiteSpace(request.Text))
            return BadRequest(new { message = "Texto da nota é obrigatório." });

        var ownerUserId = await HttpContext.GetRequiredOwnerUserIdAsync(_mediator, cancellationToken);

        var command = new CreateTextNoteCommand
        {
            ContactId = contactId,
            AuthorId = ownerUserId,
            Text = request.Text,
            StructuredData = request.StructuredData
        };

        var noteId = await _mediator.Send(command, cancellationToken);
        return CreatedAtAction(
            nameof(GetNoteById),
            new { id = noteId },
            new { noteId });
    }

    /// <summary>
    /// Atualiza uma nota existente.
    /// </summary>
    [HttpPut("notes/{id}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateNote(
        Guid id,
        [FromBody] UpdateNoteRequest request,
        CancellationToken cancellationToken)
    {
        if (request == null)
            return BadRequest(new { message = "Requisição inválida." });

        if (string.IsNullOrWhiteSpace(request.RawContent) && string.IsNullOrWhiteSpace(request.StructuredData))
            return BadRequest(new { message = "Pelo menos um campo (RawContent ou StructuredData) deve ser fornecido." });

        var ownerUserId = await HttpContext.GetRequiredOwnerUserIdAsync(_mediator, cancellationToken);

        var command = new UpdateNoteCommand
        {
            NoteId = id,
            OwnerUserId = ownerUserId,
            RawContent = request.RawContent,
            StructuredData = request.StructuredData
        };

        await _mediator.Send(command, cancellationToken);
        return NoContent();
    }

    /// <summary>
    /// Obtém o arquivo de áudio de uma nota de áudio.
    /// </summary>
    [HttpGet("notes/{id}/audio")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetAudioNoteFile(
        Guid id,
        CancellationToken cancellationToken)
    {
        if (id == Guid.Empty)
        {
            return BadRequest(new { message = "NoteId é obrigatório." });
        }

        var ownerUserId = await HttpContext.GetRequiredOwnerUserIdAsync(_mediator, cancellationToken);

        // Buscar a nota
        var note = await _noteRepository.GetByIdAsync(id, cancellationToken);
        if (note == null)
        {
            return NotFound(new { message = "Nota não encontrada." });
        }

        // Verificar se é uma nota de áudio
        if (note.Type != NoteType.Audio)
        {
            return BadRequest(new { message = "Esta nota não é uma nota de áudio." });
        }

        // Verificar se o contato pertence ao usuário
        var contactExists = await _mediator.Send(
            new GetNoteByIdQuery { NoteId = id, OwnerUserId = ownerUserId },
            cancellationToken);
        
        if (contactExists == null)
        {
            return NotFound(new { message = "Nota não encontrada." });
        }

        // Buscar o CaptureJob de áudio que corresponde a esta nota (baseado na data de criação)
        var captureJob = await _captureJobRepository.GetAudioJobByContactIdAndDateAsync(
            note.ContactId,
            ownerUserId,
            note.CreatedAt,
            cancellationToken);

        // Se não encontrar por data, tenta buscar o mais recente como fallback
        if (captureJob == null)
        {
            captureJob = await _captureJobRepository.GetLatestAudioJobByContactIdAsync(
                note.ContactId,
                ownerUserId,
                cancellationToken);
        }

        if (captureJob == null)
        {
            return NotFound(new { message = "Arquivo de áudio não encontrado para esta nota." });
        }

        // Buscar o MediaAsset
        var mediaAsset = await _mediaAssetRepository.GetByIdAsync(
            captureJob.MediaId,
            ownerUserId,
            cancellationToken);

        if (mediaAsset == null)
        {
            return NotFound(new { message = "Arquivo de mídia não encontrado." });
        }

        // Verificar se é áudio
        if (mediaAsset.Kind != MediaKind.Audio)
        {
            return BadRequest(new { message = "O arquivo associado não é um arquivo de áudio." });
        }

        // Recuperar o arquivo: primeiro tenta do banco de dados, depois do FileStore (fallback)
        byte[]? audioBytes = null;
        
        if (mediaAsset.FileContent != null && mediaAsset.FileContent.Length > 0)
        {
            // Arquivo está armazenado no banco de dados
            audioBytes = mediaAsset.FileContent;
        }
        else if (mediaAsset.MediaRef.StorageKey.StartsWith("db/"))
        {
            // StorageKey indica que deveria estar no banco, mas não está
            _logger.LogWarning(
                "Arquivo de áudio não encontrado no banco de dados. NoteId: {NoteId}, MediaId: {MediaId}",
                note.NoteId,
                mediaAsset.MediaId);
            
            return NotFound(new { 
                message = "Arquivo de áudio não encontrado no banco de dados.",
                noteId = note.NoteId,
                mediaId = mediaAsset.MediaId
            });
        }
        else
        {
            // Fallback: tentar recuperar do FileStore (para arquivos antigos)
            audioBytes = await _fileStore.GetAsync(mediaAsset.MediaRef.StorageKey, cancellationToken);
            
            if (audioBytes == null || audioBytes.Length == 0)
            {
                _logger.LogWarning(
                    "Arquivo de áudio não encontrado no armazenamento. NoteId: {NoteId}, MediaId: {MediaId}, StorageKey: {StorageKey}",
                    note.NoteId,
                    mediaAsset.MediaId,
                    mediaAsset.MediaRef.StorageKey);
                
                return NotFound(new { 
                    message = "Arquivo de áudio não encontrado no armazenamento.",
                    noteId = note.NoteId,
                    mediaId = mediaAsset.MediaId,
                    storageKey = mediaAsset.MediaRef.StorageKey
                });
            }
        }
        
        if (audioBytes == null || audioBytes.Length == 0)
        {
            return NotFound(new { message = "Arquivo de áudio não encontrado." });
        }

        // Retornar o arquivo
        return File(
            audioBytes,
            mediaAsset.MediaRef.MimeType,
            $"audio-note-{note.NoteId}.{GetFileExtension(mediaAsset.MediaRef.MimeType)}");
    }

    private static string GetFileExtension(string mimeType)
    {
        return mimeType switch
        {
            "audio/mpeg" or "audio/mp3" => "mp3",
            "audio/wav" => "wav",
            "audio/webm" => "webm",
            "audio/ogg" => "ogg",
            _ => "mp3"
        };
    }

    /// <summary>
    /// Request DTO para criação de nota de texto.
    /// </summary>
    public sealed record CreateTextNoteRequest(
        string Text,
        string? StructuredData = null);

    /// <summary>
    /// Deleta uma nota.
    /// </summary>
    [HttpDelete("notes/{id}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteNote(
        Guid id,
        CancellationToken cancellationToken)
    {
        var ownerUserId = await HttpContext.GetRequiredOwnerUserIdAsync(_mediator, cancellationToken);

        var command = new DeleteNoteCommand
        {
            NoteId = id,
            OwnerUserId = ownerUserId
        };

        await _mediator.Send(command, cancellationToken);
        return NoContent();
    }

    /// <summary>
    /// Request DTO para atualização de nota.
    /// </summary>
    public sealed record UpdateNoteRequest(
        string? RawContent = null,
        string? StructuredData = null);
}

