using AssistenteExecutivo.Domain.Enums;

namespace AssistenteExecutivo.Application.DTOs;

public class EscrowAccountDto
{
    public Guid EscrowAccountId { get; set; }
    public Guid AgreementId { get; set; }
    public Guid OwnerUserId { get; set; }
    public string Currency { get; set; } = "BRL";
    public EscrowAccountStatus Status { get; set; }
    public string? StripeConnectedAccountId { get; set; }
    public decimal Balance { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public List<EscrowTransactionDto> Transactions { get; set; } = new();
}
