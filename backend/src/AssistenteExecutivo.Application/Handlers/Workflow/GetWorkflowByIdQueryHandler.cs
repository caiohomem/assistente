using AssistenteExecutivo.Application.DTOs;
using AssistenteExecutivo.Application.Interfaces;
using AssistenteExecutivo.Application.Queries.Workflow;
using MediatR;

namespace AssistenteExecutivo.Application.Handlers.Workflow;

public class GetWorkflowByIdQueryHandler : IRequestHandler<GetWorkflowByIdQuery, WorkflowDto?>
{
    private readonly IWorkflowRepository _workflowRepository;

    public GetWorkflowByIdQueryHandler(IWorkflowRepository workflowRepository)
    {
        _workflowRepository = workflowRepository;
    }

    public async Task<WorkflowDto?> Handle(GetWorkflowByIdQuery request, CancellationToken cancellationToken)
    {
        var workflow = await _workflowRepository.GetByIdAndOwnerAsync(request.WorkflowId, request.OwnerUserId, cancellationToken);

        if (workflow == null)
            return null;

        return new WorkflowDto
        {
            WorkflowId = workflow.WorkflowId,
            OwnerUserId = workflow.OwnerUserId,
            Name = workflow.Name,
            Description = workflow.Description,
            Trigger = new WorkflowTriggerDto
            {
                Type = workflow.Trigger.Type,
                CronExpression = workflow.Trigger.CronExpression,
                EventName = workflow.Trigger.EventName,
                ConfigJson = workflow.Trigger.ConfigJson
            },
            SpecJson = workflow.SpecJson,
            SpecVersion = workflow.SpecVersion,
            N8nWorkflowId = workflow.N8nWorkflowId,
            Status = workflow.Status,
            CreatedAt = workflow.CreatedAt,
            UpdatedAt = workflow.UpdatedAt
        };
    }
}
