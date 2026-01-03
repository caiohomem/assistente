using AssistenteExecutivo.Api.Auth;
using AssistenteExecutivo.Api.Extensions;
using AssistenteExecutivo.Application.Commands.Escrow;
using AssistenteExecutivo.Application.DTOs;
using AssistenteExecutivo.Application.Queries.Escrow;
using AssistenteExecutivo.Domain.Enums;
using MediatR;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.ComponentModel.DataAnnotations;

namespace AssistenteExecutivo.Api.Controllers;

[ApiController]
[Route("api/escrow")]
[Authorize(AuthenticationSchemes = $"{BffSessionAuthenticationDefaults.Scheme},{JwtBearerDefaults.AuthenticationScheme}")]
public class EscrowController : ControllerBase
{
    private readonly IMediator _mediator;

    public EscrowController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpPost("accounts")]
    [ProducesResponseType(typeof(Guid), StatusCodes.Status201Created)]
    public async Task<IActionResult> CreateAccount(
        [FromBody] CreateEscrowAccountRequest request,
        CancellationToken cancellationToken = default)
    {
        var ownerUserId = await HttpContext.GetRequiredOwnerUserIdAsync(_mediator, cancellationToken);

        var command = new CreateEscrowAccountCommand
        {
            EscrowAccountId = request.EscrowAccountId ?? Guid.Empty,
            AgreementId = request.AgreementId,
            OwnerUserId = ownerUserId,
            Currency = string.IsNullOrWhiteSpace(request.Currency) ? "BRL" : request.Currency!
        };

        var escrowAccountId = await _mediator.Send(command, cancellationToken);
        return CreatedAtAction(nameof(GetAccount), new { escrowAccountId }, new { escrowAccountId });
    }

    [HttpGet("accounts/{escrowAccountId:guid}")]
    [ProducesResponseType(typeof(EscrowAccountDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetAccount(
        [FromRoute] Guid escrowAccountId,
        CancellationToken cancellationToken = default)
    {
        var ownerUserId = await HttpContext.GetRequiredOwnerUserIdAsync(_mediator, cancellationToken);
        var account = await _mediator.Send(new GetEscrowAccountByIdQuery
        {
            EscrowAccountId = escrowAccountId,
            RequestingUserId = ownerUserId
        }, cancellationToken);

        if (account == null)
            return NotFound(new { message = "Conta escrow não encontrada." });

        return Ok(account);
    }

    [HttpGet("accounts/{escrowAccountId:guid}/transactions")]
    [ProducesResponseType(typeof(List<EscrowTransactionDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> ListTransactions(
        [FromRoute] Guid escrowAccountId,
        CancellationToken cancellationToken = default)
    {
        var ownerUserId = await HttpContext.GetRequiredOwnerUserIdAsync(_mediator, cancellationToken);
        var transactions = await _mediator.Send(new ListEscrowTransactionsQuery
        {
            EscrowAccountId = escrowAccountId,
            RequestingUserId = ownerUserId
        }, cancellationToken);

        return Ok(transactions);
    }

    [HttpPost("accounts/{escrowAccountId:guid}/deposit")]
    [ProducesResponseType(typeof(EscrowDepositResult), StatusCodes.Status200OK)]
    public async Task<IActionResult> Deposit(
        [FromRoute] Guid escrowAccountId,
        [FromBody] CreateEscrowDepositRequest request,
        CancellationToken cancellationToken = default)
    {
        var ownerUserId = await HttpContext.GetRequiredOwnerUserIdAsync(_mediator, cancellationToken);

        var command = new DepositToEscrowCommand
        {
            EscrowAccountId = escrowAccountId,
            TransactionId = request.TransactionId ?? Guid.Empty,
            Amount = request.Amount,
            Currency = string.IsNullOrWhiteSpace(request.Currency) ? "BRL" : request.Currency!,
            Description = request.Description,
            PaymentIntentId = request.PaymentIntentId,
            IdempotencyKey = request.IdempotencyKey,
            RequestedBy = ownerUserId
        };

        var result = await _mediator.Send(command, cancellationToken);
        return Ok(result);
    }

    [HttpPost("accounts/{escrowAccountId:guid}/payouts")]
    [ProducesResponseType(typeof(Guid), StatusCodes.Status200OK)]
    public async Task<IActionResult> RequestPayout(
        [FromRoute] Guid escrowAccountId,
        [FromBody] RequestEscrowPayoutRequest request,
        CancellationToken cancellationToken = default)
    {
        var ownerUserId = await HttpContext.GetRequiredOwnerUserIdAsync(_mediator, cancellationToken);

        var command = new RequestPayoutCommand
        {
            EscrowAccountId = escrowAccountId,
            TransactionId = request.TransactionId ?? Guid.Empty,
            PartyId = request.PartyId,
            Amount = request.Amount,
            Currency = string.IsNullOrWhiteSpace(request.Currency) ? "BRL" : request.Currency!,
            Description = request.Description,
            IdempotencyKey = request.IdempotencyKey,
            RequestedBy = ownerUserId
        };

        var transactionId = await _mediator.Send(command, cancellationToken);
        return Ok(new { transactionId });
    }

    [HttpPost("accounts/{escrowAccountId:guid}/transactions/{transactionId:guid}/approve")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> ApprovePayout(
        [FromRoute] Guid escrowAccountId,
        [FromRoute] Guid transactionId,
        CancellationToken cancellationToken = default)
    {
        var ownerUserId = await HttpContext.GetRequiredOwnerUserIdAsync(_mediator, cancellationToken);
        await _mediator.Send(new ApprovePayoutCommand
        {
            EscrowAccountId = escrowAccountId,
            TransactionId = transactionId,
            ApprovedBy = ownerUserId
        }, cancellationToken);

        return NoContent();
    }

    [HttpPost("accounts/{escrowAccountId:guid}/transactions/{transactionId:guid}/reject")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> RejectPayout(
        [FromRoute] Guid escrowAccountId,
        [FromRoute] Guid transactionId,
        [FromBody] RejectEscrowPayoutRequest request,
        CancellationToken cancellationToken = default)
    {
        var ownerUserId = await HttpContext.GetRequiredOwnerUserIdAsync(_mediator, cancellationToken);
        await _mediator.Send(new RejectPayoutCommand
        {
            EscrowAccountId = escrowAccountId,
            TransactionId = transactionId,
            RejectedBy = ownerUserId,
            Reason = request.Reason
        }, cancellationToken);

        return NoContent();
    }

    [HttpPost("accounts/{escrowAccountId:guid}/transactions/{transactionId:guid}/execute")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> ExecutePayout(
        [FromRoute] Guid escrowAccountId,
        [FromRoute] Guid transactionId,
        [FromBody] ExecuteEscrowPayoutRequest request,
        CancellationToken cancellationToken = default)
    {
        var ownerUserId = await HttpContext.GetRequiredOwnerUserIdAsync(_mediator, cancellationToken);
        await _mediator.Send(new ExecutePayoutCommand
        {
            EscrowAccountId = escrowAccountId,
            TransactionId = transactionId,
            StripeTransferId = request.StripeTransferId,
            PerformedBy = ownerUserId
        }, cancellationToken);

        return NoContent();
    }

    [HttpPost("accounts/{escrowAccountId:guid}/connect-stripe")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> ConnectStripeAccount(
        [FromRoute] Guid escrowAccountId,
        [FromBody] ConnectStripeAccountRequest request,
        CancellationToken cancellationToken = default)
    {
        var ownerUserId = await HttpContext.GetRequiredOwnerUserIdAsync(_mediator, cancellationToken);
        await _mediator.Send(new ConnectStripeAccountCommand
        {
            EscrowAccountId = escrowAccountId,
            OwnerUserId = ownerUserId,
            AuthorizationCode = request.AuthorizationCode
        }, cancellationToken);

        return NoContent();
    }
}

public record CreateEscrowAccountRequest
{
    public Guid AgreementId { get; init; }
    public Guid? EscrowAccountId { get; init; }
    public string? Currency { get; init; }
}

public record CreateEscrowDepositRequest
{
    public Guid? TransactionId { get; init; }
    [Range(0.01, double.MaxValue)]
    public decimal Amount { get; init; }
    public string? Currency { get; init; }
    public string? Description { get; init; }
    public string? PaymentIntentId { get; init; }
    public string? IdempotencyKey { get; init; }
}

public record RequestEscrowPayoutRequest
{
    public Guid? TransactionId { get; init; }
    public Guid? PartyId { get; init; }
    [Range(0.01, double.MaxValue)]
    public decimal Amount { get; init; }
    public string? Currency { get; init; }
    public string? Description { get; init; }
    public string? IdempotencyKey { get; init; }
}

public record RejectEscrowPayoutRequest
{
    [Required]
    public string Reason { get; init; } = string.Empty;
}

public record ExecuteEscrowPayoutRequest
{
    public string? StripeTransferId { get; init; }
}

public record ConnectStripeAccountRequest
{
    [Required]
    public string AuthorizationCode { get; init; } = string.Empty;
}
