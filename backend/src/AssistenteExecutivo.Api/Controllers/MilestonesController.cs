using AssistenteExecutivo.Api.Auth;
using AssistenteExecutivo.Api.Extensions;
using AssistenteExecutivo.Application.Commands.Milestones;
using AssistenteExecutivo.Application.DTOs;
using AssistenteExecutivo.Application.Queries.Milestones;
using MediatR;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.ComponentModel.DataAnnotations;

namespace AssistenteExecutivo.Api.Controllers;

[ApiController]
[Route("api/milestones")]
[Authorize(AuthenticationSchemes = $"{BffSessionAuthenticationDefaults.Scheme},{JwtBearerDefaults.AuthenticationScheme}")]
public class MilestonesController : ControllerBase
{
    private readonly IMediator _mediator;

    public MilestonesController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpGet("{milestoneId:guid}")]
    [ProducesResponseType(typeof(MilestoneDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetMilestone(
        [FromRoute] Guid milestoneId,
        CancellationToken cancellationToken = default)
    {
        var ownerUserId = await HttpContext.GetRequiredOwnerUserIdAsync(_mediator, cancellationToken);
        var milestone = await _mediator.Send(new GetMilestoneByIdQuery
        {
            MilestoneId = milestoneId,
            RequestingUserId = ownerUserId
        }, cancellationToken);

        if (milestone == null)
            return NotFound(new { message = "Milestone não encontrado." });

        return Ok(milestone);
    }

    [HttpPost("{milestoneId:guid}/complete")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> CompleteMilestone(
        [FromRoute] Guid milestoneId,
        [FromBody] CompleteMilestoneRequest request,
        CancellationToken cancellationToken = default)
    {
        var ownerUserId = await HttpContext.GetRequiredOwnerUserIdAsync(_mediator, cancellationToken);

        var command = new CompleteMilestoneCommand
        {
            AgreementId = request.AgreementId,
            MilestoneId = milestoneId,
            RequestedBy = ownerUserId,
            Notes = request.Notes,
            ReleasedPayoutTransactionId = request.ReleasedPayoutTransactionId
        };

        await _mediator.Send(command, cancellationToken);
        return NoContent();
    }

    [HttpPost("{milestoneId:guid}/trigger-payout")]
    [ProducesResponseType(typeof(Guid), StatusCodes.Status200OK)]
    public async Task<IActionResult> TriggerPayout(
        [FromRoute] Guid milestoneId,
        [FromBody] TriggerMilestonePayoutRequest request,
        CancellationToken cancellationToken = default)
    {
        var ownerUserId = await HttpContext.GetRequiredOwnerUserIdAsync(_mediator, cancellationToken);

        var command = new TriggerMilestonePayoutCommand
        {
            AgreementId = request.AgreementId,
            MilestoneId = milestoneId,
            BeneficiaryPartyId = request.BeneficiaryPartyId,
            Amount = request.Amount,
            Currency = request.Currency,
            RequestedBy = ownerUserId
        };

        var transactionId = await _mediator.Send(command, cancellationToken);
        return Ok(new { transactionId });
    }
}

public record CompleteMilestoneRequest
{
    [Required]
    public Guid AgreementId { get; init; }
    public string? Notes { get; init; }
    public Guid? ReleasedPayoutTransactionId { get; init; }
}

public record TriggerMilestonePayoutRequest
{
    [Required]
    public Guid AgreementId { get; init; }
    public Guid? BeneficiaryPartyId { get; init; }
    public decimal Amount { get; init; }
    public string? Currency { get; init; } = "BRL";
}
