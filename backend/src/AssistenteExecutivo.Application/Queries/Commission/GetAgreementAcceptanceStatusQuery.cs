using MediatR;

namespace AssistenteExecutivo.Application.Queries.Commission;

public class GetAgreementAcceptanceStatusQuery : IRequest<AgreementAcceptanceStatusDto>
{
    public Guid AgreementId { get; set; }
    public Guid OwnerUserId { get; set; }
    public int? MaxDays { get; set; }
}

public class AgreementAcceptanceStatusDto
{
    public Guid AgreementId { get; set; }
    public int TotalParties { get; set; }
    public int AcceptedParties { get; set; }
    public int PendingParties { get; set; }
    public int DaysElapsed { get; set; }
    public int MaxDays { get; set; }
    public bool AllAccepted { get; set; }
    public bool IsExpired { get; set; }
    public string Status { get; set; } = string.Empty;
}
