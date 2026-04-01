using AssistenteExecutivo.Domain.ValueObjects;

namespace AssistenteExecutivo.Domain.Interfaces;

public class PaymentIntentResult
{
    public string PaymentIntentId { get; set; } = string.Empty;
    public string ClientSecret { get; set; } = string.Empty;
}

public class PayoutResult
{
    public string Status { get; set; } = string.Empty;
    public string? TransferId { get; set; }
    public string? FailureReason { get; set; }
}

public class SplitPayoutResult
{
    public string Status { get; set; } = string.Empty;
    public List<string> TransferIds { get; set; } = new();
    public string? FailureReason { get; set; }
}

public class SubscriptionCheckoutResult
{
    public string CheckoutUrl { get; set; } = string.Empty;
    public string SessionId { get; set; } = string.Empty;
}

public interface IPaymentGateway
{
    Task<PaymentIntentResult> CreateEscrowDepositIntentAsync(
        Guid escrowAccountId,
        Money amount,
        string description,
        CancellationToken cancellationToken = default);

    Task<PayoutResult> ExecuteEscrowPayoutAsync(
        Guid escrowAccountId,
        Guid transactionId,
        Money amount,
        string destinationAccountId,
        CancellationToken cancellationToken = default);

    Task<SplitPayoutResult> ExecuteSplitPayoutAsync(
        string escrowStripeAccountId,
        List<(string destinationAccountId, decimal amount, string currency, string description)> transfers,
        CancellationToken cancellationToken = default);

    Task<string> ConnectAccountAsync(
        Guid ownerUserId,
        string authorizationCode,
        CancellationToken cancellationToken = default);

    Task<SubscriptionCheckoutResult> CreateSubscriptionCheckoutSessionAsync(
        Guid ownerUserId,
        string planCode,
        string successUrl,
        string cancelUrl,
        CancellationToken cancellationToken = default);

    Task HandleWebhookAsync(
        string payload,
        string signature,
        CancellationToken cancellationToken = default);
}
