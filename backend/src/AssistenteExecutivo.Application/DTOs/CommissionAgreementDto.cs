using AssistenteExecutivo.Domain.Enums;

namespace AssistenteExecutivo.Application.DTOs;

public class CommissionAgreementDto
{
    public Guid AgreementId { get; set; }
    public Guid OwnerUserId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? Terms { get; set; }
    public decimal TotalValue { get; set; }
    public string Currency { get; set; } = "BRL";
    public AgreementStatus Status { get; set; }
    public Guid? EscrowAccountId { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public DateTime? ActivatedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
    public DateTime? CanceledAt { get; set; }
    public List<AgreementPartyDto> Parties { get; set; } = new();
    public List<MilestoneDto> Milestones { get; set; } = new();
}
