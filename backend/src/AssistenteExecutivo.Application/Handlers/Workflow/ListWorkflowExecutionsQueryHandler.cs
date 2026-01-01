using AssistenteExecutivo.Application.DTOs;
using AssistenteExecutivo.Application.Interfaces;
using AssistenteExecutivo.Application.Queries.Workflow;
using MediatR;

namespace AssistenteExecutivo.Application.Handlers.Workflow;

public class ListWorkflowExecutionsQueryHandler : IRequestHandler<ListWorkflowExecutionsQuery, List<WorkflowExecutionSummaryDto>>
{
    private readonly IWorkflowExecutionRepository _executionRepository;
    private readonly IWorkflowRepository _workflowRepository;

    public ListWorkflowExecutionsQueryHandler(
        IWorkflowExecutionRepository executionRepository,
        IWorkflowRepository workflowRepository)
    {
        _executionRepository = executionRepository;
        _workflowRepository = workflowRepository;
    }

    public async Task<List<WorkflowExecutionSummaryDto>> Handle(ListWorkflowExecutionsQuery request, CancellationToken cancellationToken)
    {
        var executions = request.WorkflowId.HasValue
            ? await _executionRepository.GetByWorkflowIdAsync(request.WorkflowId.Value, cancellationToken)
            : await _executionRepository.GetByOwnerAsync(request.OwnerUserId, request.Limit, cancellationToken);

        // Get workflow names
        var workflowIds = executions.Select(e => e.WorkflowId).Distinct().ToList();
        var workflows = await Task.WhenAll(workflowIds.Select(id => _workflowRepository.GetByIdAsync(id, cancellationToken)));
        var workflowNames = workflows
            .Where(w => w != null)
            .ToDictionary(w => w!.WorkflowId, w => w!.Name);

        return executions.Select(e => new WorkflowExecutionSummaryDto
        {
            ExecutionId = e.ExecutionId,
            WorkflowId = e.WorkflowId,
            WorkflowName = workflowNames.TryGetValue(e.WorkflowId, out var name) ? name : "Unknown",
            Status = e.Status,
            StartedAt = e.StartedAt,
            CompletedAt = e.CompletedAt
        }).ToList();
    }
}
