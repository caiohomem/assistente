using AssistenteExecutivo.Domain.Enums;
using MediatR;

namespace AssistenteExecutivo.Application.Commands.Commission;

public class AddPartyToAgreementCommand : IRequest<Unit>
{
    public Guid AgreementId { get; set; }
    public Guid PartyId { get; set; }
    public Guid? ContactId { get; set; }
    public Guid? CompanyId { get; set; }
    public string PartyName { get; set; } = string.Empty;
    public string? Email { get; set; }
    public decimal SplitPercentage { get; set; }
    public PartyRole Role { get; set; }
    public string? StripeAccountId { get; set; }
    public Guid RequestedBy { get; set; }
}
