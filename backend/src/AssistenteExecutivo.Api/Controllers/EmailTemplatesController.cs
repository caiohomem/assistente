using AssistenteExecutivo.Api.Auth;
using AssistenteExecutivo.Application.Commands.Notifications;
using AssistenteExecutivo.Application.Queries.Notifications;
using AssistenteExecutivo.Domain.Notifications;
using MediatR;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.ComponentModel.DataAnnotations;
using EmailTemplateDto = AssistenteExecutivo.Application.Queries.Notifications.EmailTemplateDto;
using ListEmailTemplatesResultDto = AssistenteExecutivo.Application.Queries.Notifications.ListEmailTemplatesResultDto;

namespace AssistenteExecutivo.Api.Controllers;

[ApiController]
[Route("api/email-templates")]
[Authorize(AuthenticationSchemes = $"{BffSessionAuthenticationDefaults.Scheme},{JwtBearerDefaults.AuthenticationScheme}")]
public sealed class EmailTemplatesController : ControllerBase
{
    private readonly IMediator _mediator;

    public EmailTemplatesController(IMediator mediator)
    {
        _mediator = mediator;
    }

    /// <summary>
    /// Cria um novo template de email.
    /// </summary>
    [HttpPost]
    [ProducesResponseType(typeof(Guid), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> CreateEmailTemplate(
        [FromBody] CreateEmailTemplateRequest request,
        CancellationToken cancellationToken = default)
    {
        var command = new CreateEmailTemplateCommand
        {
            Name = request.Name,
            TemplateType = request.TemplateType,
            Subject = request.Subject,
            HtmlBody = request.HtmlBody
        };

        var templateId = await _mediator.Send(command, cancellationToken);
        return CreatedAtAction(nameof(GetEmailTemplateById), new { id = templateId }, templateId);
    }

    /// <summary>
    /// Lista templates de email.
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(ListEmailTemplatesResultDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> ListEmailTemplates(
        [FromQuery] EmailTemplateType? templateType = null,
        [FromQuery] bool? activeOnly = false,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        var query = new ListEmailTemplatesQuery
        {
            TemplateType = templateType,
            ActiveOnly = activeOnly,
            Page = page,
            PageSize = pageSize
        };

        var result = await _mediator.Send(query, cancellationToken);
        return Ok(result);
    }

    /// <summary>
    /// Obtém um template de email específico por ID.
    /// </summary>
    [HttpGet("{id}")]
    [ProducesResponseType(typeof(EmailTemplateDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> GetEmailTemplateById(
        [FromRoute] Guid id,
        CancellationToken cancellationToken = default)
    {
        var query = new GetEmailTemplateByIdQuery
        {
            EmailTemplateId = id
        };

        var result = await _mediator.Send(query, cancellationToken);
        if (result == null)
            return NotFound(new { message = "Template de email não encontrado." });

        return Ok(result);
    }

    /// <summary>
    /// Atualiza um template de email.
    /// </summary>
    [HttpPut("{id}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> UpdateEmailTemplate(
        [FromRoute] Guid id,
        [FromBody] UpdateEmailTemplateRequest request,
        CancellationToken cancellationToken = default)
    {
        var command = new UpdateEmailTemplateCommand
        {
            EmailTemplateId = id,
            Name = request.Name,
            Subject = request.Subject,
            HtmlBody = request.HtmlBody
        };

        await _mediator.Send(command, cancellationToken);
        return NoContent();
    }

    /// <summary>
    /// Ativa um template de email.
    /// </summary>
    [HttpPost("{id}/activate")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> ActivateEmailTemplate(
        [FromRoute] Guid id,
        CancellationToken cancellationToken = default)
    {
        var command = new ActivateEmailTemplateCommand
        {
            EmailTemplateId = id
        };

        await _mediator.Send(command, cancellationToken);
        return NoContent();
    }

    /// <summary>
    /// Desativa um template de email.
    /// </summary>
    [HttpPost("{id}/deactivate")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> DeactivateEmailTemplate(
        [FromRoute] Guid id,
        CancellationToken cancellationToken = default)
    {
        var command = new DeactivateEmailTemplateCommand
        {
            EmailTemplateId = id
        };

        await _mediator.Send(command, cancellationToken);
        return NoContent();
    }

    /// <summary>
    /// Deleta um template de email.
    /// </summary>
    [HttpDelete("{id}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> DeleteEmailTemplate(
        [FromRoute] Guid id,
        CancellationToken cancellationToken = default)
    {
        var command = new DeleteEmailTemplateCommand
        {
            EmailTemplateId = id
        };

        await _mediator.Send(command, cancellationToken);
        return NoContent();
    }
}

#region Request DTOs

public class CreateEmailTemplateRequest
{
    [Required]
    [MaxLength(200)]
    public string Name { get; set; } = string.Empty;

    [Required]
    public EmailTemplateType TemplateType { get; set; }

    [Required]
    public string Subject { get; set; } = string.Empty;

    [Required]
    public string HtmlBody { get; set; } = string.Empty;
}

public class UpdateEmailTemplateRequest
{
    [MaxLength(200)]
    public string? Name { get; set; }

    public string? Subject { get; set; }

    public string? HtmlBody { get; set; }
}

#endregion

