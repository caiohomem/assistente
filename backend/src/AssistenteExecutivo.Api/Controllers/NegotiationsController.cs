using AssistenteExecutivo.Api.Auth;
using AssistenteExecutivo.Api.Extensions;
using AssistenteExecutivo.Application.Commands.Negotiations;
using AssistenteExecutivo.Application.DTOs;
using AssistenteExecutivo.Application.Queries.Negotiations;
using AssistenteExecutivo.Domain.Enums;
using MediatR;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.ComponentModel.DataAnnotations;

namespace AssistenteExecutivo.Api.Controllers;

[ApiController]
[Route("api/negotiations")]
[Authorize(AuthenticationSchemes = $"{BffSessionAuthenticationDefaults.Scheme},{JwtBearerDefaults.AuthenticationScheme}")]
public class NegotiationsController : ControllerBase
{
    private readonly IMediator _mediator;

    public NegotiationsController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpGet]
    [ProducesResponseType(typeof(List<NegotiationSessionDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> ListSessions(
        [FromQuery] NegotiationStatus? status = null,
        CancellationToken cancellationToken = default)
    {
        var ownerUserId = await HttpContext.GetRequiredOwnerUserIdAsync(_mediator, cancellationToken);
        var query = new ListNegotiationSessionsQuery
        {
            OwnerUserId = ownerUserId,
            Status = status
        };
        var sessions = await _mediator.Send(query, cancellationToken);
        return Ok(sessions);
    }

    [HttpGet("{sessionId}")]
    [ProducesResponseType(typeof(NegotiationSessionDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetSession(
        [FromRoute] Guid sessionId,
        CancellationToken cancellationToken = default)
    {
        var ownerUserId = await HttpContext.GetRequiredOwnerUserIdAsync(_mediator, cancellationToken);
        var query = new GetNegotiationSessionByIdQuery
        {
            SessionId = sessionId,
            RequestingUserId = ownerUserId
        };

        var session = await _mediator.Send(query, cancellationToken);
        if (session == null)
            return NotFound(new { message = "Sessão não encontrada" });

        return Ok(session);
    }

    [HttpGet("{sessionId}/proposals")]
    [ProducesResponseType(typeof(List<NegotiationProposalDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> ListProposals(
        [FromRoute] Guid sessionId,
        CancellationToken cancellationToken = default)
    {
        var ownerUserId = await HttpContext.GetRequiredOwnerUserIdAsync(_mediator, cancellationToken);
        var query = new ListProposalsBySessionQuery
        {
            SessionId = sessionId,
            RequestingUserId = ownerUserId
        };
        var proposals = await _mediator.Send(query, cancellationToken);
        return Ok(proposals);
    }

    [HttpPost]
    [ProducesResponseType(typeof(Guid), StatusCodes.Status201Created)]
    public async Task<IActionResult> CreateSession(
        [FromBody] CreateNegotiationSessionRequest request,
        CancellationToken cancellationToken = default)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        var ownerUserId = await HttpContext.GetRequiredOwnerUserIdAsync(_mediator, cancellationToken);
        var command = new CreateNegotiationSessionCommand
        {
            SessionId = request.SessionId ?? Guid.Empty,
            OwnerUserId = ownerUserId,
            Title = request.Title,
            Context = request.Context
        };

        var sessionId = await _mediator.Send(command, cancellationToken);
        return CreatedAtAction(nameof(GetSession), new { sessionId }, new { sessionId });
    }

    [HttpPost("{sessionId}/proposals")]
    [ProducesResponseType(typeof(Guid), StatusCodes.Status201Created)]
    public async Task<IActionResult> SubmitProposal(
        [FromRoute] Guid sessionId,
        [FromBody] SubmitProposalRequest request,
        CancellationToken cancellationToken = default)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        var command = new SubmitProposalCommand
        {
            SessionId = sessionId,
            ProposalId = request.ProposalId ?? Guid.Empty,
            PartyId = request.PartyId,
            Source = request.Source ?? ProposalSource.Party,
            Content = request.Content
        };

        var proposalId = await _mediator.Send(command, cancellationToken);
        return CreatedAtAction(nameof(ListProposals), new { sessionId }, new { proposalId });
    }

    [HttpPost("{sessionId}/proposals/ai-suggest")]
    [ProducesResponseType(typeof(Guid), StatusCodes.Status202Accepted)]
    public async Task<IActionResult> RequestAiSuggestion(
        [FromRoute] Guid sessionId,
        [FromBody] RequestAiProposalRequest request,
        CancellationToken cancellationToken = default)
    {
        var ownerUserId = await HttpContext.GetRequiredOwnerUserIdAsync(_mediator, cancellationToken);
        var command = new RequestAIProposalCommand
        {
            SessionId = sessionId,
            RequestedBy = ownerUserId,
            AdditionalInstructions = request.Instructions
        };

        var proposalId = await _mediator.Send(command, cancellationToken);
        return Accepted(new { proposalId });
    }

    [HttpPost("{sessionId}/proposals/{proposalId}/accept")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> AcceptProposal(
        [FromRoute] Guid sessionId,
        [FromRoute] Guid proposalId,
        [FromBody] AcceptProposalRequest request,
        CancellationToken cancellationToken = default)
    {
        var command = new AcceptProposalCommand
        {
            SessionId = sessionId,
            ProposalId = proposalId,
            ActingPartyId = request.ActingPartyId
        };

        await _mediator.Send(command, cancellationToken);
        return NoContent();
    }

    [HttpPost("{sessionId}/proposals/{proposalId}/reject")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> RejectProposal(
        [FromRoute] Guid sessionId,
        [FromRoute] Guid proposalId,
        [FromBody] RejectProposalRequest request,
        CancellationToken cancellationToken = default)
    {
        var command = new RejectProposalCommand
        {
            SessionId = sessionId,
            ProposalId = proposalId,
            ActingPartyId = request.ActingPartyId,
            Reason = request.Reason
        };

        await _mediator.Send(command, cancellationToken);
        return NoContent();
    }

    [HttpPost("{sessionId}/generate-agreement")]
    [ProducesResponseType(typeof(Guid), StatusCodes.Status200OK)]
    public async Task<IActionResult> GenerateAgreement(
        [FromRoute] Guid sessionId,
        [FromBody] GenerateAgreementRequest request,
        CancellationToken cancellationToken = default)
    {
        var ownerUserId = await HttpContext.GetRequiredOwnerUserIdAsync(_mediator, cancellationToken);
        var command = new GenerateAgreementFromNegotiationCommand
        {
            SessionId = sessionId,
            AgreementId = request.AgreementId,
            OwnerUserId = ownerUserId
        };

        var agreementId = await _mediator.Send(command, cancellationToken);
        return Ok(new { agreementId });
    }
}

public record CreateNegotiationSessionRequest
{
    public Guid? SessionId { get; init; }
    [Required]
    public string Title { get; init; } = string.Empty;
    public string? Context { get; init; }
}

public record SubmitProposalRequest
{
    public Guid? ProposalId { get; init; }
    public Guid? PartyId { get; init; }
    public ProposalSource? Source { get; init; }
    [Required]
    public string Content { get; init; } = string.Empty;
}

public record RequestAiProposalRequest
{
    public string? Instructions { get; init; }
}

public record AcceptProposalRequest
{
    public Guid? ActingPartyId { get; init; }
}

public record RejectProposalRequest
{
    public Guid? ActingPartyId { get; init; }
    [Required]
    public string Reason { get; init; } = string.Empty;
}

public record GenerateAgreementRequest
{
    [Required]
    public Guid AgreementId { get; init; }
}
