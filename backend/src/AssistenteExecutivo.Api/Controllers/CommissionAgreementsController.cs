using AssistenteExecutivo.Api.Auth;
using AssistenteExecutivo.Api.Extensions;
using AssistenteExecutivo.Application.Commands.Commission;
using AssistenteExecutivo.Application.Commands.Milestones;
using AssistenteExecutivo.Application.DTOs;
using AssistenteExecutivo.Application.Queries.Commission;
using AssistenteExecutivo.Domain.Enums;
using MediatR;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.ComponentModel.DataAnnotations;

namespace AssistenteExecutivo.Api.Controllers;

[ApiController]
[Route("api/commission-agreements")]
[Authorize(AuthenticationSchemes = $"{BffSessionAuthenticationDefaults.Scheme},{JwtBearerDefaults.AuthenticationScheme}")]
public class CommissionAgreementsController : ControllerBase
{
    private readonly IMediator _mediator;

    public CommissionAgreementsController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpGet]
    [ProducesResponseType(typeof(List<CommissionAgreementDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> ListAgreements(
        [FromQuery] AgreementStatus? status = null,
        CancellationToken cancellationToken = default)
    {
        var ownerUserId = await HttpContext.GetRequiredOwnerUserIdAsync(_mediator, cancellationToken);

        var query = new ListAgreementsQuery
        {
            OwnerUserId = ownerUserId,
            Status = status
        };

        var result = await _mediator.Send(query, cancellationToken);
        return Ok(result);
    }

    [HttpGet("{agreementId:guid}")]
    [ProducesResponseType(typeof(CommissionAgreementDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetAgreement(
        [FromRoute] Guid agreementId,
        CancellationToken cancellationToken = default)
    {
        var ownerUserId = await HttpContext.GetRequiredOwnerUserIdAsync(_mediator, cancellationToken);

        var query = new GetAgreementByIdQuery
        {
            AgreementId = agreementId,
            RequestingUserId = ownerUserId
        };

        var agreement = await _mediator.Send(query, cancellationToken);
        if (agreement == null)
            return NotFound(new { message = "Acordo não encontrado." });

        return Ok(agreement);
    }

    [HttpPost]
    [ProducesResponseType(typeof(Guid), StatusCodes.Status201Created)]
    public async Task<IActionResult> CreateAgreement(
        [FromBody] CreateCommissionAgreementRequest request,
        CancellationToken cancellationToken = default)
    {
        var ownerUserId = await HttpContext.GetRequiredOwnerUserIdAsync(_mediator, cancellationToken);

        var command = new CreateCommissionAgreementCommand
        {
            AgreementId = Guid.Empty,
            OwnerUserId = ownerUserId,
            Title = request.Title,
            Description = request.Description,
            Terms = request.Terms,
            TotalValue = request.TotalValue,
            Currency = string.IsNullOrWhiteSpace(request.Currency) ? "BRL" : request.Currency!
        };

        var agreementId = await _mediator.Send(command, cancellationToken);
        return CreatedAtAction(nameof(GetAgreement), new { agreementId }, new { agreementId });
    }

    [HttpPost("{agreementId:guid}/parties")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> AddParty(
        [FromRoute] Guid agreementId,
        [FromBody] AddAgreementPartyRequest request,
        CancellationToken cancellationToken = default)
    {
        var ownerUserId = await HttpContext.GetRequiredOwnerUserIdAsync(_mediator, cancellationToken);

        var command = new AddPartyToAgreementCommand
        {
            AgreementId = agreementId,
            PartyId = request.PartyId ?? Guid.Empty,
            ContactId = request.ContactId,
            CompanyId = request.CompanyId,
            PartyName = request.PartyName,
            Email = request.Email,
            SplitPercentage = request.SplitPercentage,
            Role = request.Role,
            RequestedBy = ownerUserId
        };

        await _mediator.Send(command, cancellationToken);
        return NoContent();
    }

    [HttpPost("{agreementId:guid}/parties/{partyId:guid}/accept")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> AcceptAgreementAsParty(
        [FromRoute] Guid agreementId,
        [FromRoute] Guid partyId,
        CancellationToken cancellationToken = default)
    {
        var command = new AcceptAgreementAsPartyCommand
        {
            AgreementId = agreementId,
            PartyId = partyId
        };

        await _mediator.Send(command, cancellationToken);
        return NoContent();
    }

    [HttpPost("{agreementId:guid}/activate")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> ActivateAgreement(
        [FromRoute] Guid agreementId,
        CancellationToken cancellationToken = default)
    {
        var ownerUserId = await HttpContext.GetRequiredOwnerUserIdAsync(_mediator, cancellationToken);

        var command = new ActivateAgreementCommand
        {
            AgreementId = agreementId,
            RequestedBy = ownerUserId
        };

        await _mediator.Send(command, cancellationToken);
        return NoContent();
    }

    [HttpPost("{agreementId:guid}/complete")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> CompleteAgreement(
        [FromRoute] Guid agreementId,
        CancellationToken cancellationToken = default)
    {
        var ownerUserId = await HttpContext.GetRequiredOwnerUserIdAsync(_mediator, cancellationToken);

        var command = new CompleteAgreementCommand
        {
            AgreementId = agreementId,
            RequestedBy = ownerUserId
        };

        await _mediator.Send(command, cancellationToken);
        return NoContent();
    }

    [HttpPost("{agreementId:guid}/dispute")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> DisputeAgreement(
        [FromRoute] Guid agreementId,
        [FromBody] DisputeAgreementRequest request,
        CancellationToken cancellationToken = default)
    {
        var ownerUserId = await HttpContext.GetRequiredOwnerUserIdAsync(_mediator, cancellationToken);

        var command = new DisputeAgreementCommand
        {
            AgreementId = agreementId,
            RequestedBy = ownerUserId,
            Reason = request.Reason
        };

        await _mediator.Send(command, cancellationToken);
        return NoContent();
    }

    [HttpPost("{agreementId:guid}/cancel")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> CancelAgreement(
        [FromRoute] Guid agreementId,
        [FromBody] CancelAgreementRequest request,
        CancellationToken cancellationToken = default)
    {
        var ownerUserId = await HttpContext.GetRequiredOwnerUserIdAsync(_mediator, cancellationToken);

        var command = new CancelAgreementCommand
        {
            AgreementId = agreementId,
            RequestedBy = ownerUserId,
            Reason = request.Reason
        };

        await _mediator.Send(command, cancellationToken);
        return NoContent();
    }

    [HttpPost("{agreementId:guid}/milestones")]
    [ProducesResponseType(typeof(Guid), StatusCodes.Status201Created)]
    public async Task<IActionResult> CreateMilestone(
        [FromRoute] Guid agreementId,
        [FromBody] CreateMilestoneRequest request,
        CancellationToken cancellationToken = default)
    {
        var ownerUserId = await HttpContext.GetRequiredOwnerUserIdAsync(_mediator, cancellationToken);

        var command = new CreateMilestoneCommand
        {
            AgreementId = agreementId,
            MilestoneId = request.MilestoneId ?? Guid.Empty,
            Description = request.Description,
            Value = request.Value,
            Currency = string.IsNullOrWhiteSpace(request.Currency) ? "BRL" : request.Currency!,
            DueDate = request.DueDate,
            RequestedBy = ownerUserId
        };

        var milestoneId = await _mediator.Send(command, cancellationToken);
        return CreatedAtAction(nameof(GetAgreement), new { agreementId }, new { milestoneId });
    }
}

public record CreateCommissionAgreementRequest
{
    [Required]
    public string Title { get; init; } = string.Empty;
    public string? Description { get; init; }
    public string? Terms { get; init; }
    [Range(0.01, double.MaxValue)]
    public decimal TotalValue { get; init; }
    public string? Currency { get; init; } = "BRL";
}

public record AddAgreementPartyRequest
{
    public Guid? PartyId { get; init; }
    public Guid? ContactId { get; init; }
    public Guid? CompanyId { get; init; }
    [Required]
    public string PartyName { get; init; } = string.Empty;
    [EmailAddress]
    public string? Email { get; init; }
    [Range(0, 100)]
    public decimal SplitPercentage { get; init; }
    [Required]
    public PartyRole Role { get; init; } = PartyRole.Agent;
}

public record DisputeAgreementRequest
{
    [Required]
    public string Reason { get; init; } = string.Empty;
}

public record CancelAgreementRequest
{
    [Required]
    public string Reason { get; init; } = string.Empty;
}

public record CreateMilestoneRequest
{
    public Guid? MilestoneId { get; init; }
    [Required]
    public string Description { get; init; } = string.Empty;
    [Range(0.01, double.MaxValue)]
    public decimal Value { get; init; }
    public string? Currency { get; init; } = "BRL";
    [Required]
    public DateTime DueDate { get; init; }
}
