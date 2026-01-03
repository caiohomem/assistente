using AssistenteExecutivo.Application.DTOs;
using MediatR;

namespace AssistenteExecutivo.Application.Queries.Milestones;

public class GetMilestoneByIdQuery : IRequest<MilestoneDto?>
{
    public Guid MilestoneId { get; set; }
    public Guid RequestingUserId { get; set; }
}
