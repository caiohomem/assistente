using MediatR;

namespace AssistenteExecutivo.Application.Commands.Milestones;

public class CompleteMilestoneCommand : IRequest<Unit>
{
    public Guid AgreementId { get; set; }
    public Guid MilestoneId { get; set; }
    public Guid RequestedBy { get; set; }
    public string? Notes { get; set; }
    public Guid? ReleasedPayoutTransactionId { get; set; }
}
