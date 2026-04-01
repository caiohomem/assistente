namespace AssistenteExecutivo.Infrastructure.Services.Payments;

public class StripeOptions
{
    public string ApiKey { get; set; } = string.Empty;
    public string WebhookSecret { get; set; } = string.Empty;
    public string ConnectClientId { get; set; } = string.Empty;
    public string DefaultCurrency { get; set; } = "brl";
}
