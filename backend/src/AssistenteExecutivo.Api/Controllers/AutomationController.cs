using System.ComponentModel.DataAnnotations;
using AssistenteExecutivo.Api.Auth;
using AssistenteExecutivo.Api.Extensions;
using AssistenteExecutivo.Application.Commands.Automation;
using AssistenteExecutivo.Application.Queries.Automation;
using AssistenteExecutivo.Domain.Enums;
using MediatR;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AssistenteExecutivo.Api.Controllers;

[ApiController]
[Route("api/automation")]
[Authorize(AuthenticationSchemes = $"{BffSessionAuthenticationDefaults.Scheme},{JwtBearerDefaults.AuthenticationScheme}")]
public sealed class AutomationController : ControllerBase
{
    private readonly IMediator _mediator;

    public AutomationController(IMediator mediator)
    {
        _mediator = mediator;
    }

    #region Reminders

    /// <summary>
    /// Cria um novo lembrete.
    /// </summary>
    [HttpPost("reminders")]
    [ProducesResponseType(typeof(Guid), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> CreateReminder(
        [FromBody] CreateReminderRequest request,
        CancellationToken cancellationToken = default)
    {
        var ownerUserId = await HttpContext.GetRequiredOwnerUserIdAsync(_mediator, cancellationToken);

        var command = new CreateReminderCommand
        {
            OwnerUserId = ownerUserId,
            ContactId = request.ContactId,
            Reason = request.Reason,
            SuggestedMessage = request.SuggestedMessage,
            ScheduledFor = request.ScheduledFor
        };

        var reminderId = await _mediator.Send(command, cancellationToken);
        return CreatedAtAction(nameof(GetReminderById), new { id = reminderId }, reminderId);
    }

    /// <summary>
    /// Lista lembretes do usuário autenticado.
    /// </summary>
    [HttpGet("reminders")]
    [ProducesResponseType(typeof(ListRemindersResultDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> ListReminders(
        [FromQuery] Guid? contactId = null,
        [FromQuery] ReminderStatus? status = null,
        [FromQuery] DateTime? startDate = null,
        [FromQuery] DateTime? endDate = null,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        var ownerUserId = await HttpContext.GetRequiredOwnerUserIdAsync(_mediator, cancellationToken);

        var query = new ListRemindersQuery
        {
            OwnerUserId = ownerUserId,
            ContactId = contactId,
            Status = status,
            StartDate = startDate,
            EndDate = endDate,
            Page = page,
            PageSize = pageSize
        };

        var result = await _mediator.Send(query, cancellationToken);
        return Ok(result);
    }

    /// <summary>
    /// Obtém um lembrete específico por ID.
    /// </summary>
    [HttpGet("reminders/{id}")]
    [ProducesResponseType(typeof(ReminderDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> GetReminderById(
        [FromRoute] Guid id,
        CancellationToken cancellationToken = default)
    {
        var ownerUserId = await HttpContext.GetRequiredOwnerUserIdAsync(_mediator, cancellationToken);

        var query = new GetReminderByIdQuery
        {
            ReminderId = id,
            OwnerUserId = ownerUserId
        };

        var result = await _mediator.Send(query, cancellationToken);
        if (result == null)
            return NotFound(new { message = "Lembrete não encontrado." });

        return Ok(result);
    }

    /// <summary>
    /// Atualiza o status de um lembrete.
    /// </summary>
    [HttpPut("reminders/{id}/status")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> UpdateReminderStatus(
        [FromRoute] Guid id,
        [FromBody] UpdateReminderStatusRequest request,
        CancellationToken cancellationToken = default)
    {
        var ownerUserId = await HttpContext.GetRequiredOwnerUserIdAsync(_mediator, cancellationToken);

        var command = new UpdateReminderStatusCommand
        {
            ReminderId = id,
            OwnerUserId = ownerUserId,
            NewStatus = request.NewStatus,
            NewScheduledFor = request.NewScheduledFor
        };

        await _mediator.Send(command, cancellationToken);
        return NoContent();
    }

    /// <summary>
    /// Deleta um lembrete.
    /// </summary>
    [HttpDelete("reminders/{id}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> DeleteReminder(
        [FromRoute] Guid id,
        CancellationToken cancellationToken = default)
    {
        var ownerUserId = await HttpContext.GetRequiredOwnerUserIdAsync(_mediator, cancellationToken);

        var command = new DeleteReminderCommand
        {
            ReminderId = id,
            OwnerUserId = ownerUserId
        };

        await _mediator.Send(command, cancellationToken);
        return NoContent();
    }

    #endregion

    #region Drafts

    /// <summary>
    /// Cria um novo draft de documento.
    /// </summary>
    [HttpPost("drafts")]
    [ProducesResponseType(typeof(Guid), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> CreateDraft(
        [FromBody] CreateDraftRequest request,
        CancellationToken cancellationToken = default)
    {
        var ownerUserId = await HttpContext.GetRequiredOwnerUserIdAsync(_mediator, cancellationToken);

        var command = new CreateDraftDocumentCommand
        {
            OwnerUserId = ownerUserId,
            DocumentType = request.DocumentType,
            Content = request.Content,
            ContactId = request.ContactId,
            CompanyId = request.CompanyId,
            TemplateId = request.TemplateId,
            LetterheadId = request.LetterheadId
        };

        var draftId = await _mediator.Send(command, cancellationToken);
        return CreatedAtAction(nameof(GetDraftById), new { id = draftId }, draftId);
    }

    /// <summary>
    /// Lista drafts do usuário autenticado.
    /// </summary>
    [HttpGet("drafts")]
    [ProducesResponseType(typeof(ListDraftsResultDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> ListDrafts(
        [FromQuery] Guid? contactId = null,
        [FromQuery] Guid? companyId = null,
        [FromQuery] DocumentType? documentType = null,
        [FromQuery] DraftStatus? status = null,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        var ownerUserId = await HttpContext.GetRequiredOwnerUserIdAsync(_mediator, cancellationToken);

        var query = new ListDraftsQuery
        {
            OwnerUserId = ownerUserId,
            ContactId = contactId,
            CompanyId = companyId,
            DocumentType = documentType,
            Status = status,
            Page = page,
            PageSize = pageSize
        };

        var result = await _mediator.Send(query, cancellationToken);
        return Ok(result);
    }

    /// <summary>
    /// Obtém um draft específico por ID.
    /// </summary>
    [HttpGet("drafts/{id}")]
    [ProducesResponseType(typeof(DraftDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> GetDraftById(
        [FromRoute] Guid id,
        CancellationToken cancellationToken = default)
    {
        var ownerUserId = await HttpContext.GetRequiredOwnerUserIdAsync(_mediator, cancellationToken);

        var query = new GetDraftByIdQuery
        {
            DraftId = id,
            OwnerUserId = ownerUserId
        };

        var result = await _mediator.Send(query, cancellationToken);
        if (result == null)
            return NotFound(new { message = "Draft não encontrado." });

        return Ok(result);
    }

    /// <summary>
    /// Atualiza um draft.
    /// </summary>
    [HttpPut("drafts/{id}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> UpdateDraft(
        [FromRoute] Guid id,
        [FromBody] UpdateDraftRequest request,
        CancellationToken cancellationToken = default)
    {
        var ownerUserId = await HttpContext.GetRequiredOwnerUserIdAsync(_mediator, cancellationToken);

        var command = new UpdateDraftDocumentCommand
        {
            DraftId = id,
            OwnerUserId = ownerUserId,
            Content = request.Content,
            ContactId = request.ContactId,
            CompanyId = request.CompanyId,
            TemplateId = request.TemplateId,
            LetterheadId = request.LetterheadId
        };

        await _mediator.Send(command, cancellationToken);
        return NoContent();
    }

    /// <summary>
    /// Aprova um draft.
    /// </summary>
    [HttpPost("drafts/{id}/approve")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> ApproveDraft(
        [FromRoute] Guid id,
        CancellationToken cancellationToken = default)
    {
        var ownerUserId = await HttpContext.GetRequiredOwnerUserIdAsync(_mediator, cancellationToken);

        var command = new ApproveDraftCommand
        {
            DraftId = id,
            OwnerUserId = ownerUserId
        };

        await _mediator.Send(command, cancellationToken);
        return NoContent();
    }

    /// <summary>
    /// Envia um draft.
    /// </summary>
    [HttpPost("drafts/{id}/send")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> SendDraft(
        [FromRoute] Guid id,
        CancellationToken cancellationToken = default)
    {
        var ownerUserId = await HttpContext.GetRequiredOwnerUserIdAsync(_mediator, cancellationToken);

        var command = new SendDraftCommand
        {
            DraftId = id,
            OwnerUserId = ownerUserId
        };

        await _mediator.Send(command, cancellationToken);
        return NoContent();
    }

    /// <summary>
    /// Deleta um draft.
    /// </summary>
    [HttpDelete("drafts/{id}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> DeleteDraft(
        [FromRoute] Guid id,
        CancellationToken cancellationToken = default)
    {
        var ownerUserId = await HttpContext.GetRequiredOwnerUserIdAsync(_mediator, cancellationToken);

        var command = new DeleteDraftCommand
        {
            DraftId = id,
            OwnerUserId = ownerUserId
        };

        await _mediator.Send(command, cancellationToken);
        return NoContent();
    }

    #endregion

    #region Templates

    /// <summary>
    /// Cria um novo template.
    /// </summary>
    [HttpPost("templates")]
    [ProducesResponseType(typeof(Guid), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> CreateTemplate(
        [FromBody] CreateTemplateRequest request,
        CancellationToken cancellationToken = default)
    {
        var ownerUserId = await HttpContext.GetRequiredOwnerUserIdAsync(_mediator, cancellationToken);

        var command = new CreateTemplateCommand
        {
            OwnerUserId = ownerUserId,
            Name = request.Name,
            Type = request.Type,
            Body = request.Body,
            PlaceholdersSchema = request.PlaceholdersSchema
        };

        var templateId = await _mediator.Send(command, cancellationToken);
        return CreatedAtAction(nameof(GetTemplateById), new { id = templateId }, templateId);
    }

    /// <summary>
    /// Lista templates do usuário autenticado.
    /// </summary>
    [HttpGet("templates")]
    [ProducesResponseType(typeof(ListTemplatesResultDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> ListTemplates(
        [FromQuery] TemplateType? type = null,
        [FromQuery] bool? activeOnly = false,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        var ownerUserId = await HttpContext.GetRequiredOwnerUserIdAsync(_mediator, cancellationToken);

        var query = new ListTemplatesQuery
        {
            OwnerUserId = ownerUserId,
            Type = type,
            ActiveOnly = activeOnly,
            Page = page,
            PageSize = pageSize
        };

        var result = await _mediator.Send(query, cancellationToken);
        return Ok(result);
    }

    /// <summary>
    /// Obtém um template específico por ID.
    /// </summary>
    [HttpGet("templates/{id}")]
    [ProducesResponseType(typeof(TemplateDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> GetTemplateById(
        [FromRoute] Guid id,
        CancellationToken cancellationToken = default)
    {
        var ownerUserId = await HttpContext.GetRequiredOwnerUserIdAsync(_mediator, cancellationToken);

        var query = new GetTemplateByIdQuery
        {
            TemplateId = id,
            OwnerUserId = ownerUserId
        };

        var result = await _mediator.Send(query, cancellationToken);
        if (result == null)
            return NotFound(new { message = "Template não encontrado." });

        return Ok(result);
    }

    /// <summary>
    /// Atualiza um template.
    /// </summary>
    [HttpPut("templates/{id}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> UpdateTemplate(
        [FromRoute] Guid id,
        [FromBody] UpdateTemplateRequest request,
        CancellationToken cancellationToken = default)
    {
        var ownerUserId = await HttpContext.GetRequiredOwnerUserIdAsync(_mediator, cancellationToken);

        var command = new UpdateTemplateCommand
        {
            TemplateId = id,
            OwnerUserId = ownerUserId,
            Name = request.Name,
            Body = request.Body,
            PlaceholdersSchema = request.PlaceholdersSchema,
            Active = request.Active
        };

        await _mediator.Send(command, cancellationToken);
        return NoContent();
    }

    /// <summary>
    /// Deleta um template.
    /// </summary>
    [HttpDelete("templates/{id}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> DeleteTemplate(
        [FromRoute] Guid id,
        CancellationToken cancellationToken = default)
    {
        var ownerUserId = await HttpContext.GetRequiredOwnerUserIdAsync(_mediator, cancellationToken);

        var command = new DeleteTemplateCommand
        {
            TemplateId = id,
            OwnerUserId = ownerUserId
        };

        await _mediator.Send(command, cancellationToken);
        return NoContent();
    }

    #endregion

    #region Letterheads

    /// <summary>
    /// Cria um novo papel timbrado.
    /// </summary>
    [HttpPost("letterheads")]
    [ProducesResponseType(typeof(Guid), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> CreateLetterhead(
        [FromBody] CreateLetterheadRequest request,
        CancellationToken cancellationToken = default)
    {
        var ownerUserId = await HttpContext.GetRequiredOwnerUserIdAsync(_mediator, cancellationToken);

        var command = new CreateLetterheadCommand
        {
            OwnerUserId = ownerUserId,
            Name = request.Name,
            DesignData = request.DesignData
        };

        var letterheadId = await _mediator.Send(command, cancellationToken);
        return CreatedAtAction(nameof(GetLetterheadById), new { id = letterheadId }, letterheadId);
    }

    /// <summary>
    /// Lista papéis timbrados do usuário autenticado.
    /// </summary>
    [HttpGet("letterheads")]
    [ProducesResponseType(typeof(ListLetterheadsResultDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> ListLetterheads(
        [FromQuery] bool? activeOnly = false,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        var ownerUserId = await HttpContext.GetRequiredOwnerUserIdAsync(_mediator, cancellationToken);

        var query = new ListLetterheadsQuery
        {
            OwnerUserId = ownerUserId,
            ActiveOnly = activeOnly,
            Page = page,
            PageSize = pageSize
        };

        var result = await _mediator.Send(query, cancellationToken);
        return Ok(result);
    }

    /// <summary>
    /// Obtém um papel timbrado específico por ID.
    /// </summary>
    [HttpGet("letterheads/{id}")]
    [ProducesResponseType(typeof(LetterheadDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> GetLetterheadById(
        [FromRoute] Guid id,
        CancellationToken cancellationToken = default)
    {
        var ownerUserId = await HttpContext.GetRequiredOwnerUserIdAsync(_mediator, cancellationToken);

        var query = new GetLetterheadByIdQuery
        {
            LetterheadId = id,
            OwnerUserId = ownerUserId
        };

        var result = await _mediator.Send(query, cancellationToken);
        if (result == null)
            return NotFound(new { message = "Papel timbrado não encontrado." });

        return Ok(result);
    }

    /// <summary>
    /// Atualiza um papel timbrado.
    /// </summary>
    [HttpPut("letterheads/{id}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> UpdateLetterhead(
        [FromRoute] Guid id,
        [FromBody] UpdateLetterheadRequest request,
        CancellationToken cancellationToken = default)
    {
        var ownerUserId = await HttpContext.GetRequiredOwnerUserIdAsync(_mediator, cancellationToken);

        var command = new UpdateLetterheadCommand
        {
            LetterheadId = id,
            OwnerUserId = ownerUserId,
            Name = request.Name,
            DesignData = request.DesignData,
            IsActive = request.IsActive
        };

        await _mediator.Send(command, cancellationToken);
        return NoContent();
    }

    /// <summary>
    /// Deleta um papel timbrado.
    /// </summary>
    [HttpDelete("letterheads/{id}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> DeleteLetterhead(
        [FromRoute] Guid id,
        CancellationToken cancellationToken = default)
    {
        var ownerUserId = await HttpContext.GetRequiredOwnerUserIdAsync(_mediator, cancellationToken);

        var command = new DeleteLetterheadCommand
        {
            LetterheadId = id,
            OwnerUserId = ownerUserId
        };

        await _mediator.Send(command, cancellationToken);
        return NoContent();
    }

    #endregion
}

#region Request DTOs

public class CreateReminderRequest
{
    [Required]
    public Guid ContactId { get; set; }
    
    [Required]
    [MaxLength(500)]
    public string Reason { get; set; } = string.Empty;
    
    [MaxLength(2000)]
    public string? SuggestedMessage { get; set; }
    
    [Required]
    public DateTime ScheduledFor { get; set; }
}

public class UpdateReminderStatusRequest
{
    [Required]
    public ReminderStatus NewStatus { get; set; }
    
    public DateTime? NewScheduledFor { get; set; }
}

public class CreateDraftRequest
{
    [Required]
    public DocumentType DocumentType { get; set; }
    
    [Required]
    public string Content { get; set; } = string.Empty;
    
    public Guid? ContactId { get; set; }
    
    public Guid? CompanyId { get; set; }
    
    public Guid? TemplateId { get; set; }
    
    public Guid? LetterheadId { get; set; }
}

public class UpdateDraftRequest
{
    public string? Content { get; set; }
    
    public Guid? ContactId { get; set; }
    
    public Guid? CompanyId { get; set; }
    
    public Guid? TemplateId { get; set; }
    
    public Guid? LetterheadId { get; set; }
}

public class CreateTemplateRequest
{
    [Required]
    [MaxLength(200)]
    public string Name { get; set; } = string.Empty;
    
    [Required]
    public TemplateType Type { get; set; }
    
    [Required]
    public string Body { get; set; } = string.Empty;
    
    public string? PlaceholdersSchema { get; set; }
}

public class UpdateTemplateRequest
{
    [MaxLength(200)]
    public string? Name { get; set; }
    
    public string? Body { get; set; }
    
    public string? PlaceholdersSchema { get; set; }
    
    public bool? Active { get; set; }
}

public class CreateLetterheadRequest
{
    [Required]
    [MaxLength(200)]
    public string Name { get; set; } = string.Empty;
    
    [Required]
    public string DesignData { get; set; } = string.Empty;
}

public class UpdateLetterheadRequest
{
    [MaxLength(200)]
    public string? Name { get; set; }
    
    public string? DesignData { get; set; }
    
    public bool? IsActive { get; set; }
}

#endregion

