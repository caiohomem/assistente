using AssistenteExecutivo.Api.Auth;
using AssistenteExecutivo.Application.Commands.AgentConfiguration;
using AssistenteExecutivo.Application.DTOs;
using AssistenteExecutivo.Application.Queries.AgentConfiguration;
using MediatR;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AssistenteExecutivo.Api.Controllers;

[ApiController]
[Route("api/agent-configuration")]
[Authorize(AuthenticationSchemes = $"{BffSessionAuthenticationDefaults.Scheme},{JwtBearerDefaults.AuthenticationScheme}")]
public sealed class AgentConfigurationController : ControllerBase
{
    private readonly IMediator _mediator;

    public AgentConfigurationController(IMediator mediator)
    {
        _mediator = mediator;
    }

    /// <summary>
    /// Obtém a configuração atual do agente.
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(AgentConfigurationDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> GetCurrent(CancellationToken cancellationToken = default)
    {
        var query = new GetAgentConfigurationQuery();
        var configuration = await _mediator.Send(query, cancellationToken);

        if (configuration == null)
        {
            return NotFound(new { message = "Configuração do agente não encontrada" });
        }

        return Ok(configuration);
    }

    /// <summary>
    /// Atualiza ou cria a configuração do agente.
    /// </summary>
    [HttpPut]
    [ProducesResponseType(typeof(AgentConfigurationDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(AgentConfigurationDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> UpdateOrCreate(
        [FromBody] UpdateAgentConfigurationDto dto,
        CancellationToken cancellationToken = default)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        // Verificar se já existe antes de atualizar
        var existing = await _mediator.Send(new GetAgentConfigurationQuery(), cancellationToken);
        var wasCreated = existing == null;

        var command = new UpdateAgentConfigurationCommand
        {
            OcrPrompt = dto.OcrPrompt,
            TranscriptionPrompt = dto.TranscriptionPrompt,
            WorkflowPrompt = dto.WorkflowPrompt
        };

        var result = await _mediator.Send(command, cancellationToken);

        if (wasCreated)
        {
            return CreatedAtAction(nameof(GetCurrent), result);
        }

        return Ok(result);
    }
}
