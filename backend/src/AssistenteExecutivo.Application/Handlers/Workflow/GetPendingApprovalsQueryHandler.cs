using AssistenteExecutivo.Application.DTOs;
using AssistenteExecutivo.Application.Interfaces;
using AssistenteExecutivo.Application.Queries.Workflow;
using MediatR;

namespace AssistenteExecutivo.Application.Handlers.Workflow;

public class GetPendingApprovalsQueryHandler : IRequestHandler<GetPendingApprovalsQuery, List<WorkflowExecutionDto>>
{
    private readonly IWorkflowExecutionRepository _executionRepository;

    public GetPendingApprovalsQueryHandler(IWorkflowExecutionRepository executionRepository)
    {
        _executionRepository = executionRepository;
    }

    public async Task<List<WorkflowExecutionDto>> Handle(GetPendingApprovalsQuery request, CancellationToken cancellationToken)
    {
        var executions = await _executionRepository.GetPendingApprovalsAsync(request.OwnerUserId, cancellationToken);

        return executions.Select(e => new WorkflowExecutionDto
        {
            ExecutionId = e.ExecutionId,
            WorkflowId = e.WorkflowId,
            OwnerUserId = e.OwnerUserId,
            SpecVersionUsed = e.SpecVersionUsed,
            InputJson = e.InputJson,
            OutputJson = e.OutputJson,
            Status = e.Status,
            N8nExecutionId = e.N8nExecutionId,
            ErrorMessage = e.ErrorMessage,
            CurrentStepIndex = e.CurrentStepIndex,
            StartedAt = e.StartedAt,
            CompletedAt = e.CompletedAt
        }).ToList();
    }
}
