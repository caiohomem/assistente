using AssistenteExecutivo.Application.DTOs;
using MediatR;

namespace AssistenteExecutivo.Application.Queries.Workflow;

public class ListWorkflowExecutionsQuery : IRequest<List<WorkflowExecutionSummaryDto>>
{
    public Guid OwnerUserId { get; set; }
    public Guid? WorkflowId { get; set; }
    public int Limit { get; set; } = 50;
}
