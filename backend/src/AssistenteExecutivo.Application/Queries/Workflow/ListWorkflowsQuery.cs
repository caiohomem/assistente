using AssistenteExecutivo.Application.DTOs;
using AssistenteExecutivo.Domain.Enums;
using MediatR;

namespace AssistenteExecutivo.Application.Queries.Workflow;

public class ListWorkflowsQuery : IRequest<List<WorkflowSummaryDto>>
{
    public Guid OwnerUserId { get; set; }
    public WorkflowStatus? FilterByStatus { get; set; }
}
