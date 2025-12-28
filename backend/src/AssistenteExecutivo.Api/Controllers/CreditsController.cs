using AssistenteExecutivo.Api.Extensions;
using AssistenteExecutivo.Application.Commands.Credits;
using AssistenteExecutivo.Application.DTOs;
using AssistenteExecutivo.Application.Queries.Credits;
using AssistenteExecutivo.Domain.Enums;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AssistenteExecutivo.Api.Controllers;

[ApiController]
[Route("api/credits")]
[Authorize]
public sealed class CreditsController : ControllerBase
{
    private readonly IMediator _mediator;

    public CreditsController(IMediator mediator)
    {
        _mediator = mediator;
    }

    /// <summary>
    /// Obtém o saldo de créditos do usuário autenticado
    /// </summary>
    [HttpGet("balance")]
    public async Task<ActionResult<CreditBalanceDto>> GetBalance(CancellationToken cancellationToken)
    {
        var userId = await HttpContext.GetRequiredOwnerUserIdAsync(_mediator, cancellationToken);

        var query = new GetCreditBalanceQuery
        {
            OwnerUserId = userId
        };

        var result = await _mediator.Send(query, cancellationToken);

        if (result == null)
        {
            // Wallet não existe ainda, retornar saldo zero
            return Ok(new CreditBalanceDto
            {
                OwnerUserId = userId,
                Balance = 0,
                CreatedAt = DateTime.UtcNow,
                TransactionCount = 0
            });
        }

        return Ok(result);
    }

    /// <summary>
    /// Lista as transações de crédito do usuário autenticado
    /// </summary>
    [HttpGet("transactions")]
    public async Task<ActionResult<List<CreditTransactionDto>>> GetTransactions(
        [FromQuery] CreditTransactionType? type = null,
        [FromQuery] DateTime? fromDate = null,
        [FromQuery] DateTime? toDate = null,
        [FromQuery] int? limit = null,
        [FromQuery] int? offset = null,
        CancellationToken cancellationToken = default)
    {
        var userId = await HttpContext.GetRequiredOwnerUserIdAsync(_mediator, cancellationToken);

        var query = new ListCreditTransactionsQuery
        {
            OwnerUserId = userId,
            Type = type,
            FromDate = fromDate,
            ToDate = toDate,
            Limit = limit,
            Offset = offset
        };

        var result = await _mediator.Send(query, cancellationToken);
        return Ok(result);
    }

    /// <summary>
    /// Concede créditos a um usuário (apenas administradores)
    /// </summary>
    [HttpPost("grant")]
    [Authorize(Roles = "admin")]
    public async Task<ActionResult<GrantCreditsResult>> GrantCredits(
        [FromBody] GrantCreditsRequest request,
        CancellationToken cancellationToken)
    {
        if (request == null)
        {
            return BadRequest(new { message = "Requisição inválida." });
        }

        if (request.Amount <= 0)
        {
            return BadRequest(new { message = "O valor deve ser maior que zero." });
        }

        if (request.OwnerUserId == Guid.Empty)
        {
            return BadRequest(new { message = "OwnerUserId é obrigatório." });
        }

        var command = new GrantCreditsCommand
        {
            OwnerUserId = request.OwnerUserId,
            Amount = request.Amount,
            Reason = request.Reason
        };

        var result = await _mediator.Send(command, cancellationToken);
        return Ok(result);
    }

    /// <summary>
    /// Lista os pacotes de créditos disponíveis
    /// </summary>
    [HttpGet("packages")]
    [ProducesResponseType(typeof(List<CreditPackageDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<List<CreditPackageDto>>> ListPackages(
        [FromQuery] bool includeInactive = false,
        CancellationToken cancellationToken = default)
    {
        var query = new ListCreditPackagesQuery
        {
            IncludeInactive = includeInactive
        };

        var result = await _mediator.Send(query, cancellationToken);
        return Ok(result);
    }

    /// <summary>
    /// Compra um pacote de créditos (adiciona créditos diretamente, sem gateway de pagamento)
    /// </summary>
    [HttpPost("purchase")]
    [ProducesResponseType(typeof(PurchaseCreditPackageResult), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<PurchaseCreditPackageResult>> PurchasePackage(
        [FromBody] PurchaseCreditPackageRequest request,
        CancellationToken cancellationToken)
    {
        if (request == null)
        {
            return BadRequest(new { message = "Requisição inválida." });
        }

        if (request.PackageId == Guid.Empty)
        {
            return BadRequest(new { message = "PackageId é obrigatório." });
        }

        var userId = await HttpContext.GetRequiredOwnerUserIdAsync(_mediator, cancellationToken);

        var command = new PurchaseCreditPackageCommand
        {
            OwnerUserId = userId,
            PackageId = request.PackageId
        };

        try
        {
            var result = await _mediator.Send(command, cancellationToken);
            return Ok(result);
        }
        catch (InvalidOperationException ex)
        {
            return NotFound(new { message = ex.Message });
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    /// <summary>
    /// Request DTO para comprar pacote de créditos
    /// </summary>
    public sealed record PurchaseCreditPackageRequest
    {
        public Guid PackageId { get; init; }
    }

    /// <summary>
    /// Request DTO para conceder créditos
    /// </summary>
    public sealed record GrantCreditsRequest
    {
        public Guid OwnerUserId { get; init; }
        public decimal Amount { get; init; }
        public string? Reason { get; init; }
    }
}

