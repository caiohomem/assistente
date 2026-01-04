using MediatR;

namespace AssistenteExecutivo.Application.Queries.Commission;

public class GetAgreementAcceptancePreviewQuery : IRequest<AgreementAcceptancePreviewDto?>
{
    public string Token { get; set; } = string.Empty;
}

public class AgreementAcceptancePreviewDto
{
    public Guid AgreementId { get; set; }
    public Guid PartyId { get; set; }
    public Guid OwnerUserId { get; set; }
    public int MaxDays { get; set; }
    public string AgreementTitle { get; set; } = string.Empty;
    public string PartyName { get; set; } = string.Empty;
    public DateTime ExpiresAt { get; set; }
    public bool HasAccepted { get; set; }
}
