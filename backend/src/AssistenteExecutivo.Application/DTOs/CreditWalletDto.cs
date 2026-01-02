using AssistenteExecutivo.Domain.Enums;

namespace AssistenteExecutivo.Application.DTOs;

public class CreditBalanceDto
{
    public Guid OwnerUserId { get; set; }
    public decimal Balance { get; set; }
    public DateTime CreatedAt { get; set; }
    public int TransactionCount { get; set; }
}

public class CreditTransactionDto
{
    public Guid TransactionId { get; set; }
    public Guid OwnerUserId { get; set; }
    public CreditTransactionType Type { get; set; }
    public decimal Amount { get; set; }
    public string? Reason { get; set; }
    public DateTime OccurredAt { get; set; }
    public string? IdempotencyKey { get; set; }
}














