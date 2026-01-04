using AssistenteExecutivo.Domain.Enums;

namespace AssistenteExecutivo.Application.DTOs;

public class AgreementPartyDto
{
    public Guid PartyId { get; set; }
    public Guid? ContactId { get; set; }
    public Guid? CompanyId { get; set; }
    public string PartyName { get; set; } = string.Empty;
    public string? Email { get; set; }
    public decimal SplitPercentage { get; set; }
    public PartyRole Role { get; set; }
    public string? StripeAccountId { get; set; }
    public bool HasAccepted { get; set; }
    public DateTime? AcceptedAt { get; set; }
}
