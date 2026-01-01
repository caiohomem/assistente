using AssistenteExecutivo.Application.DTOs;
using AssistenteExecutivo.Application.Interfaces;
using AssistenteExecutivo.Application.Queries.Workflow;
using MediatR;

namespace AssistenteExecutivo.Application.Handlers.Workflow;

public class ListWorkflowsQueryHandler : IRequestHandler<ListWorkflowsQuery, List<WorkflowSummaryDto>>
{
    private readonly IWorkflowRepository _workflowRepository;

    public ListWorkflowsQueryHandler(IWorkflowRepository workflowRepository)
    {
        _workflowRepository = workflowRepository;
    }

    public async Task<List<WorkflowSummaryDto>> Handle(ListWorkflowsQuery request, CancellationToken cancellationToken)
    {
        var workflows = request.FilterByStatus.HasValue
            ? await _workflowRepository.GetByOwnerAndStatusAsync(request.OwnerUserId, request.FilterByStatus.Value, cancellationToken)
            : await _workflowRepository.GetByOwnerAsync(request.OwnerUserId, cancellationToken);

        return workflows.Select(w => new WorkflowSummaryDto
        {
            WorkflowId = w.WorkflowId,
            Name = w.Name,
            Description = w.Description,
            TriggerType = w.Trigger.Type,
            Status = w.Status,
            SpecVersion = w.SpecVersion,
            CreatedAt = w.CreatedAt,
            UpdatedAt = w.UpdatedAt
        }).ToList();
    }
}
