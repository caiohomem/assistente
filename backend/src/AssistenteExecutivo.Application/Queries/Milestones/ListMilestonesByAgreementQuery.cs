using AssistenteExecutivo.Application.DTOs;
using MediatR;

namespace AssistenteExecutivo.Application.Queries.Milestones;

public class ListMilestonesByAgreementQuery : IRequest<List<MilestoneDto>>
{
    public Guid AgreementId { get; set; }
    public Guid RequestingUserId { get; set; }
}
