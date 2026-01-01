using AssistenteExecutivo.Application.DTOs;
using MediatR;

namespace AssistenteExecutivo.Application.Queries.Workflow;

public class GetWorkflowByIdQuery : IRequest<WorkflowDto?>
{
    public Guid WorkflowId { get; set; }
    public Guid OwnerUserId { get; set; }
}
