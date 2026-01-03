namespace AssistenteExecutivo.Application.DTOs;

public class EscrowDepositResult
{
    public Guid TransactionId { get; set; }
    public string PaymentIntentId { get; set; } = string.Empty;
    public string ClientSecret { get; set; } = string.Empty;
}
