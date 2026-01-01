using AssistenteExecutivo.Application.Commands.Workflow;
using AssistenteExecutivo.Application.Interfaces;
using AssistenteExecutivo.Domain.Enums;
using AssistenteExecutivo.Domain.Interfaces;
using MediatR;

namespace AssistenteExecutivo.Application.Handlers.Workflow;

public class ApproveWorkflowStepCommandHandler : IRequestHandler<ApproveWorkflowStepCommand, ApproveStepResult>
{
    private readonly IWorkflowExecutionRepository _executionRepository;
    private readonly IN8nProvider _n8nProvider;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IClock _clock;
    private readonly IPublisher _publisher;

    public ApproveWorkflowStepCommandHandler(
        IWorkflowExecutionRepository executionRepository,
        IN8nProvider n8nProvider,
        IUnitOfWork unitOfWork,
        IClock clock,
        IPublisher publisher)
    {
        _executionRepository = executionRepository;
        _n8nProvider = n8nProvider;
        _unitOfWork = unitOfWork;
        _clock = clock;
        _publisher = publisher;
    }

    public async Task<ApproveStepResult> Handle(ApproveWorkflowStepCommand request, CancellationToken cancellationToken)
    {
        // 1. Get execution
        var execution = await _executionRepository.GetByIdAsync(request.ExecutionId, cancellationToken);
        if (execution == null)
        {
            return ApproveStepResult.Failed("Execution not found");
        }

        // 2. Check status
        if (execution.Status != WorkflowExecutionStatus.WaitingApproval)
        {
            return ApproveStepResult.Failed($"Execution is not waiting for approval (current status: {execution.Status})");
        }

        // 3. Verify ownership
        if (execution.OwnerUserId != request.ApprovedByUserId)
        {
            return ApproveStepResult.Failed("Only the workflow owner can approve steps");
        }

        // 4. Resume execution in n8n
        if (!string.IsNullOrEmpty(execution.N8nExecutionId))
        {
            try
            {
                await _n8nProvider.ResumeExecutionAsync(execution.N8nExecutionId, cancellationToken);
            }
            catch (Exception ex)
            {
                return ApproveStepResult.Failed($"Failed to resume execution in n8n: {ex.Message}");
            }
        }

        // 5. Update execution status
        execution.ApproveStep(request.ApprovedByUserId, _clock);

        // 6. Persist
        await _executionRepository.UpdateAsync(execution, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        // 7. Publish domain events
        foreach (var domainEvent in execution.DomainEvents)
        {
            await _publisher.Publish(domainEvent, cancellationToken);
        }
        execution.ClearDomainEvents();

        return ApproveStepResult.Succeeded();
    }
}
