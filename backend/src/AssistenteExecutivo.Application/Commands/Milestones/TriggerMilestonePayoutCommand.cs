using AssistenteExecutivo.Domain.Enums;
using MediatR;

namespace AssistenteExecutivo.Application.Commands.Milestones;

public class TriggerMilestonePayoutCommand : IRequest<Guid>
{
    public Guid AgreementId { get; set; }
    public Guid MilestoneId { get; set; }
    public Guid? BeneficiaryPartyId { get; set; }
    public decimal Amount { get; set; }
    public string Currency { get; set; } = "BRL";
    public PayoutApprovalType ApprovalType { get; set; } = PayoutApprovalType.ApprovalRequired;
    public Guid RequestedBy { get; set; }
}
