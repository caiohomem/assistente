using AssistenteExecutivo.Domain.Enums;
using MediatR;
using System.Collections.Generic;

namespace AssistenteExecutivo.Application.Queries.Commission;

public class GetAgreementAcceptancePendingPartiesQuery : IRequest<AgreementAcceptancePendingPartiesDto>
{
    public Guid AgreementId { get; set; }
    public Guid OwnerUserId { get; set; }
    public string ApiBaseUrl { get; set; } = string.Empty;
    public int? MaxDays { get; set; }
    public bool IncludeAccepted { get; set; }
    public string? TemplateName { get; set; }
}

public class AgreementAcceptancePendingPartiesDto
{
    public Guid AgreementId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? Terms { get; set; }
    public decimal TotalValue { get; set; }
    public string Currency { get; set; } = "BRL";
    public DateTime ExpiresAt { get; set; }
    public int MaxDays { get; set; }
    public List<AgreementAcceptancePartyDto> Parties { get; set; } = new();
}

public class AgreementAcceptancePartyDto
{
    public Guid PartyId { get; set; }
    public string PartyName { get; set; } = string.Empty;
    public string? Email { get; set; }
    public decimal SplitPercentage { get; set; }
    public PartyRole Role { get; set; }
    public bool HasAccepted { get; set; }
    public DateTime? AcceptedAt { get; set; }
    public string? AcceptUrl { get; set; }
    public string? EmailSubject { get; set; }
    public string? EmailHtml { get; set; }
}
