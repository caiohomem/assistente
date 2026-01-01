using AssistenteExecutivo.Application.DTOs;
using MediatR;

namespace AssistenteExecutivo.Application.Queries.Workflow;

public class GetPendingApprovalsQuery : IRequest<List<WorkflowExecutionDto>>
{
    public Guid OwnerUserId { get; set; }
}
