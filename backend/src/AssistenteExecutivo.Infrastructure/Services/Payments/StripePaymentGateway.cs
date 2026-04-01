using System.Collections.Generic;
using AssistenteExecutivo.Domain.Interfaces;
using AssistenteExecutivo.Domain.ValueObjects;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Stripe;
using Stripe.Checkout;

namespace AssistenteExecutivo.Infrastructure.Services.Payments;

public class StripePaymentGateway : IPaymentGateway
{
    private readonly StripeClient _client;
    private readonly StripeOptions _options;
    private readonly ILogger<StripePaymentGateway> _logger;

    public StripePaymentGateway(
        IOptions<StripeOptions> options,
        ILogger<StripePaymentGateway> logger)
    {
        _options = options.Value ?? throw new InvalidOperationException("Stripe options não configuradas.");
        if (string.IsNullOrWhiteSpace(_options.ApiKey))
            throw new InvalidOperationException("Stripe:ApiKey não configurada.");

        _client = new StripeClient(_options.ApiKey);
        _logger = logger;
    }

    public async Task<PaymentIntentResult> CreateEscrowDepositIntentAsync(
        Guid escrowAccountId,
        Money amount,
        string description,
        CancellationToken cancellationToken = default)
    {
        var service = new PaymentIntentService(_client);
        var createOptions = new PaymentIntentCreateOptions
        {
            Amount = ConvertToStripeAmount(amount),
            Currency = amount.Currency.ToLowerInvariant(),
            Description = description,
            AutomaticPaymentMethods = new PaymentIntentAutomaticPaymentMethodsOptions
            {
                Enabled = true
            },
            Metadata = new Dictionary<string, string>
            {
                ["type"] = "escrow_deposit",
                ["escrow_account_id"] = escrowAccountId.ToString()
            }
        };

        var intent = await service.CreateAsync(createOptions, requestOptions: null, cancellationToken: cancellationToken);
        return new PaymentIntentResult
        {
            PaymentIntentId = intent.Id,
            ClientSecret = intent.ClientSecret ?? string.Empty
        };
    }

    public async Task<PayoutResult> ExecuteEscrowPayoutAsync(
        Guid escrowAccountId,
        Guid transactionId,
        Money amount,
        string destinationAccountId,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var transferService = new TransferService(_client);
            var transfer = await transferService.CreateAsync(new TransferCreateOptions
            {
                Amount = ConvertToStripeAmount(amount),
                Currency = amount.Currency.ToLowerInvariant(),
                Destination = destinationAccountId,
                Metadata = new Dictionary<string, string>
                {
                    ["type"] = "escrow_payout",
                    ["escrow_account_id"] = escrowAccountId.ToString(),
                    ["transaction_id"] = transactionId.ToString()
                }
            }, requestOptions: null, cancellationToken: cancellationToken);

            var status = transfer.Reversed ? "reversed" : "succeeded";

            return new PayoutResult
            {
                Status = status,
                TransferId = transfer.Id
            };
        }
        catch (StripeException ex)
        {
            _logger.LogError(ex, "Erro Stripe ao executar payout para escrow {EscrowAccountId}", escrowAccountId);
            throw;
        }
    }

    public async Task<string> ConnectAccountAsync(
        Guid ownerUserId,
        string authorizationCode,
        CancellationToken cancellationToken = default)
    {
        var oauthService = new OAuthTokenService(_client);
        var token = await oauthService.CreateAsync(new OAuthTokenCreateOptions
        {
            GrantType = "authorization_code",
            Code = authorizationCode
        }, requestOptions: null, cancellationToken: cancellationToken);

        if (string.IsNullOrWhiteSpace(token.StripeUserId))
            throw new InvalidOperationException("Stripe não retornou connected account id.");

        _logger.LogInformation("Stripe connect concluído para usuário {OwnerUserId}", ownerUserId);
        return token.StripeUserId;
    }

    public async Task<SubscriptionCheckoutResult> CreateSubscriptionCheckoutSessionAsync(
        Guid ownerUserId,
        string planCode,
        string successUrl,
        string cancelUrl,
        CancellationToken cancellationToken = default)
    {
        var sessionService = new SessionService(_client);
        var session = await sessionService.CreateAsync(new SessionCreateOptions
        {
            Mode = "subscription",
            SuccessUrl = successUrl,
            CancelUrl = cancelUrl,
            CustomerEmail = null,
            LineItems = new List<SessionLineItemOptions>
            {
                new()
                {
                    Price = planCode,
                    Quantity = 1
                }
            },
            Metadata = new Dictionary<string, string>
            {
                ["owner_user_id"] = ownerUserId.ToString()
            }
        }, requestOptions: null, cancellationToken: cancellationToken);

        return new SubscriptionCheckoutResult
        {
            CheckoutUrl = session.Url ?? string.Empty,
            SessionId = session.Id
        };
    }

    public Task HandleWebhookAsync(
        string payload,
        string signature,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(_options.WebhookSecret))
        {
            _logger.LogWarning("Stripe webhook secret não configurado.");
            return Task.CompletedTask;
        }

        Event stripeEvent;
        try
        {
            stripeEvent = EventUtility.ConstructEvent(
                payload,
                signature,
                _options.WebhookSecret);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Falha ao validar webhook do Stripe.");
            throw;
        }

        _logger.LogInformation("Stripe webhook recebido: {Type}", stripeEvent.Type);
        return Task.CompletedTask;
    }

    private static long ConvertToStripeAmount(Money amount)
    {
        return (long)Math.Round(amount.Amount * 100, MidpointRounding.AwayFromZero);
    }
}
