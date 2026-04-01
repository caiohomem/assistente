using MediatR;

namespace AssistenteExecutivo.Application.Commands.Milestones;

public class CreateMilestoneCommand : IRequest<Guid>
{
    public Guid AgreementId { get; set; }
    public Guid MilestoneId { get; set; }
    public string Description { get; set; } = string.Empty;
    public decimal Value { get; set; }
    public string Currency { get; set; } = "BRL";
    public DateTime DueDate { get; set; }
    public Guid RequestedBy { get; set; }
}
