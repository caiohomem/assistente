using AssistenteExecutivo.Api.Auth;
using AssistenteExecutivo.Api.Extensions;
using AssistenteExecutivo.Application.Commands.Assistant;
using MediatR;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AssistenteExecutivo.Api.Controllers;

[ApiController]
[Route("api/assistant")]
[Authorize(AuthenticationSchemes = $"{BffSessionAuthenticationDefaults.Scheme},{JwtBearerDefaults.AuthenticationScheme}")]
public sealed class AssistantController : ControllerBase
{
    private readonly IMediator _mediator;

    public AssistantController(IMediator mediator)
    {
        _mediator = mediator;
    }

    /// <summary>
    /// Processa uma mensagem do assistente com IA usando function calling.
    /// </summary>
    [HttpPost("chat")]
    [ProducesResponseType(typeof(ProcessAssistantChatResult), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> ProcessChat(
        [FromBody] ProcessAssistantChatRequest request,
        CancellationToken cancellationToken = default)
    {
        var ownerUserId = await HttpContext.GetRequiredOwnerUserIdAsync(_mediator, cancellationToken);

        var command = new ProcessAssistantChatCommand
        {
            OwnerUserId = ownerUserId,
            Messages = request.Messages,
            Model = request.Model
        };

        var result = await _mediator.Send(command, cancellationToken);
        return Ok(result);
    }
}

public sealed record ProcessAssistantChatRequest
{
    public List<ChatMessage> Messages { get; init; } = new();
    public string? Model { get; init; }
}

