using AssistenteExecutivo.Domain.Enums;

namespace AssistenteExecutivo.Application.DTOs;

public class MilestoneDto
{
    public Guid MilestoneId { get; set; }
    public Guid AgreementId { get; set; }
    public string Description { get; set; } = string.Empty;
    public decimal Value { get; set; }
    public string Currency { get; set; } = "BRL";
    public DateTime DueDate { get; set; }
    public MilestoneStatus Status { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
    public string? CompletionNotes { get; set; }
    public Guid? ReleasedPayoutTransactionId { get; set; }
}
