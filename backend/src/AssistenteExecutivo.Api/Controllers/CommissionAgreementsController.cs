using AssistenteExecutivo.Api.Auth;
using AssistenteExecutivo.Api.Extensions;
using AssistenteExecutivo.Application.Commands.Commission;
using AssistenteExecutivo.Application.Commands.Milestones;
using AssistenteExecutivo.Application.DTOs;
using AssistenteExecutivo.Application.Interfaces;
using AssistenteExecutivo.Application.Queries.Commission;
using AssistenteExecutivo.Domain.Enums;
using MediatR;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using System.ComponentModel.DataAnnotations;

namespace AssistenteExecutivo.Api.Controllers;

[ApiController]
[Route("api/commission-agreements")]
[Authorize(AuthenticationSchemes = $"{BffSessionAuthenticationDefaults.Scheme},{JwtBearerDefaults.AuthenticationScheme}")]
public class CommissionAgreementsController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly IAgreementAcceptanceTokenService _tokenService;
    private readonly IConfiguration _configuration;

    public CommissionAgreementsController(
        IMediator mediator,
        IAgreementAcceptanceTokenService tokenService,
        IConfiguration configuration)
    {
        _mediator = mediator;
        _tokenService = tokenService;
        _configuration = configuration;
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
            StripeAccountId = request.StripeAccountId,
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

    [HttpPost("{agreementId:guid}/parties/{partyId:guid}/connect-stripe")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> ConnectPartyStripeAccount(
        [FromRoute] Guid agreementId,
        [FromRoute] Guid partyId,
        [FromBody] ConnectPartyStripeRequest request,
        CancellationToken cancellationToken = default)
    {
        var ownerUserId = await HttpContext.GetRequiredOwnerUserIdAsync(_mediator, cancellationToken);

        var command = new ConnectPartyStripeAccountCommand
        {
            AgreementId = agreementId,
            PartyId = partyId,
            AuthorizationCodeOrAccountId = request.AuthorizationCodeOrAccountId,
            RequestedBy = ownerUserId
        };

        await _mediator.Send(command, cancellationToken);
        return NoContent();
    }

    [AllowAnonymous]
    [HttpGet("accept")]
    public async Task<IActionResult> AcceptAgreementByToken(
        [FromQuery] string token,
        CancellationToken cancellationToken = default)
    {
        var frontendBaseUrl = _configuration["Frontend:BaseUrl"] ?? "/";
        if (!_tokenService.TryValidateToken(token, out var payload))
        {
            var errorUrl = $"{frontendBaseUrl.TrimEnd('/')}/acordos/aceite?status=invalid";
            return Redirect(errorUrl);
        }

        try
        {
            var command = new AcceptAgreementAsPartyCommand
            {
                AgreementId = payload.AgreementId,
                PartyId = payload.PartyId
            };

            await _mediator.Send(command, cancellationToken);
            var successUrl = $"{frontendBaseUrl.TrimEnd('/')}/acordos/aceite?status=success&token={Uri.EscapeDataString(token)}";
            return Redirect(successUrl);
        }
        catch
        {
            var failUrl = $"{frontendBaseUrl.TrimEnd('/')}/acordos/aceite?status=failed&token={Uri.EscapeDataString(token)}";
            return Redirect(failUrl);
        }
    }

    [AllowAnonymous]
    [HttpGet("acceptance/preview")]
    [ProducesResponseType(typeof(AgreementAcceptancePreviewDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> GetAcceptancePreview(
        [FromQuery] string token,
        CancellationToken cancellationToken = default)
    {
        var preview = await _mediator.Send(new GetAgreementAcceptancePreviewQuery
        {
            Token = token
        }, cancellationToken);

        if (preview == null)
        {
            return BadRequest(new { message = "Token inválido ou expirado." });
        }

        return Ok(preview);
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

    [HttpGet("{agreementId:guid}/acceptance/pending")]
    [ProducesResponseType(typeof(AgreementAcceptancePendingPartiesDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetPendingAcceptanceParties(
        [FromRoute] Guid agreementId,
        [FromQuery] Guid? ownerUserId,
        [FromQuery] int? maxDays,
        [FromQuery] string? templateName,
        [FromQuery] bool includeAccepted = false,
        CancellationToken cancellationToken = default)
    {
        var resolvedOwnerUserId = await ResolveOwnerUserIdAsync(ownerUserId, cancellationToken);
        var frontendBaseUrl = _configuration["Frontend:BaseUrl"];
        if (string.IsNullOrWhiteSpace(frontendBaseUrl))
        {
            throw new ArgumentException("Frontend:BaseUrl é obrigatório para gerar links de aceite.");
        }

        var payload = await _mediator.Send(new GetAgreementAcceptancePendingPartiesQuery
        {
            AgreementId = agreementId,
            OwnerUserId = resolvedOwnerUserId,
            ApiBaseUrl = frontendBaseUrl,
            MaxDays = maxDays,
            IncludeAccepted = includeAccepted,
            TemplateName = templateName
        }, cancellationToken);

        return Ok(payload);
    }

    [HttpGet("{agreementId:guid}/acceptance/status")]
    [ProducesResponseType(typeof(AgreementAcceptanceStatusDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAcceptanceStatus(
        [FromRoute] Guid agreementId,
        [FromQuery] Guid? ownerUserId,
        [FromQuery] int? maxDays,
        CancellationToken cancellationToken = default)
    {
        var resolvedOwnerUserId = await ResolveOwnerUserIdAsync(ownerUserId, cancellationToken);

        var status = await _mediator.Send(new GetAgreementAcceptanceStatusQuery
        {
            AgreementId = agreementId,
            OwnerUserId = resolvedOwnerUserId,
            MaxDays = maxDays
        }, cancellationToken);

        return Ok(status);
    }

    [HttpPost("{agreementId:guid}/acceptance/cancel")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> CancelAcceptanceAgreement(
        [FromRoute] Guid agreementId,
        [FromBody] AgreementAcceptanceCancelRequest request,
        CancellationToken cancellationToken = default)
    {
        var ownerUserId = await ResolveOwnerUserIdAsync(request.OwnerUserId, cancellationToken);

        var command = new CancelAgreementCommand
        {
            AgreementId = agreementId,
            RequestedBy = ownerUserId,
            Reason = string.IsNullOrWhiteSpace(request.Reason)
                ? "Prazo máximo de aceite expirado."
                : request.Reason
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

    private async Task<Guid> ResolveOwnerUserIdAsync(Guid? ownerUserId, CancellationToken cancellationToken)
    {
        var resolved = await HttpContext.GetOwnerUserIdAsync(_mediator, cancellationToken);
        if (resolved.HasValue)
        {
            return resolved.Value;
        }

        if (!IsServiceAccountRequest())
        {
            throw new UnauthorizedAccessException("Usuário não autenticado ou não encontrado no sistema.");
        }

        if (!ownerUserId.HasValue || ownerUserId.Value == Guid.Empty)
        {
            throw new ArgumentException("ownerUserId é obrigatório para chamadas de service account.");
        }

        return ownerUserId.Value;
    }

    private bool IsServiceAccountRequest()
    {
        var preferredUsername = User.FindFirst("preferred_username")?.Value;
        if (!string.IsNullOrWhiteSpace(preferredUsername) &&
            preferredUsername.StartsWith("service-account-", StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        var clientId = User.FindFirst("client_id")?.Value;
        if (!string.IsNullOrWhiteSpace(clientId))
        {
            return true;
        }

        return User.IsInRole("api.access");
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
    public string? Email { get; init; }
    [Range(0, 100)]
    public decimal SplitPercentage { get; init; }
    [Required]
    public PartyRole Role { get; init; } = PartyRole.Agent;
    public string? StripeAccountId { get; init; }
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

public record AgreementAcceptanceCancelRequest
{
    public Guid? OwnerUserId { get; init; }
    public string? Reason { get; init; }
}

public record ConnectPartyStripeRequest
{
    [Required]
    public string AuthorizationCodeOrAccountId { get; init; } = string.Empty;
}
