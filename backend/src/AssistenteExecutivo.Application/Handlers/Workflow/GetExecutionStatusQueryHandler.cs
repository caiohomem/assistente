using AssistenteExecutivo.Application.DTOs;
using AssistenteExecutivo.Application.Interfaces;
using AssistenteExecutivo.Application.Queries.Workflow;
using MediatR;

namespace AssistenteExecutivo.Application.Handlers.Workflow;

public class GetExecutionStatusQueryHandler : IRequestHandler<GetExecutionStatusQuery, WorkflowExecutionDto?>
{
    private readonly IWorkflowExecutionRepository _executionRepository;

    public GetExecutionStatusQueryHandler(IWorkflowExecutionRepository executionRepository)
    {
        _executionRepository = executionRepository;
    }

    public async Task<WorkflowExecutionDto?> Handle(GetExecutionStatusQuery request, CancellationToken cancellationToken)
    {
        var execution = await _executionRepository.GetByIdAndOwnerAsync(request.ExecutionId, request.OwnerUserId, cancellationToken);

        if (execution == null)
            return null;

        return new WorkflowExecutionDto
        {
            ExecutionId = execution.ExecutionId,
            WorkflowId = execution.WorkflowId,
            OwnerUserId = execution.OwnerUserId,
            SpecVersionUsed = execution.SpecVersionUsed,
            InputJson = execution.InputJson,
            OutputJson = execution.OutputJson,
            Status = execution.Status,
            N8nExecutionId = execution.N8nExecutionId,
            ErrorMessage = execution.ErrorMessage,
            CurrentStepIndex = execution.CurrentStepIndex,
            StartedAt = execution.StartedAt,
            CompletedAt = execution.CompletedAt
        };
    }
}
