using AssistenteExecutivo.Application.Commands.Workflow;
using AssistenteExecutivo.Application.Interfaces;
using AssistenteExecutivo.Domain.Entities;
using AssistenteExecutivo.Domain.Enums;
using AssistenteExecutivo.Domain.Interfaces;
using MediatR;

namespace AssistenteExecutivo.Application.Handlers.Workflow;

public class ExecuteWorkflowCommandHandler : IRequestHandler<ExecuteWorkflowCommand, ExecuteWorkflowResult>
{
    private readonly IWorkflowRepository _workflowRepository;
    private readonly IWorkflowExecutionRepository _executionRepository;
    private readonly IN8nProvider _n8nProvider;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IClock _clock;
    private readonly IPublisher _publisher;

    public ExecuteWorkflowCommandHandler(
        IWorkflowRepository workflowRepository,
        IWorkflowExecutionRepository executionRepository,
        IN8nProvider n8nProvider,
        IUnitOfWork unitOfWork,
        IClock clock,
        IPublisher publisher)
    {
        _workflowRepository = workflowRepository;
        _executionRepository = executionRepository;
        _n8nProvider = n8nProvider;
        _unitOfWork = unitOfWork;
        _clock = clock;
        _publisher = publisher;
    }

    public async Task<ExecuteWorkflowResult> Handle(ExecuteWorkflowCommand request, CancellationToken cancellationToken)
    {
        // 1. Get workflow
        var workflow = await _workflowRepository.GetByIdAndOwnerAsync(request.WorkflowId, request.OwnerUserId, cancellationToken);
        if (workflow == null)
        {
            return ExecuteWorkflowResult.Failed("Workflow not found");
        }

        // 2. Check workflow status
        if (workflow.Status != WorkflowStatus.Active)
        {
            return ExecuteWorkflowResult.Failed($"Workflow is not active (current status: {workflow.Status})");
        }

        if (string.IsNullOrEmpty(workflow.N8nWorkflowId))
        {
            return ExecuteWorkflowResult.Failed("Workflow is not compiled");
        }

        // 3. Create execution record
        var executionId = Guid.NewGuid();
        var execution = WorkflowExecution.Start(
            executionId,
            workflow.WorkflowId,
            request.OwnerUserId,
            workflow.SpecVersion,
            request.InputJson,
            _clock);

        // 4. Execute in n8n
        var n8nResult = await _n8nProvider.ExecuteWorkflowAsync(
            workflow.N8nWorkflowId,
            request.InputJson,
            cancellationToken);

        if (!n8nResult.Success)
        {
            execution.Fail(n8nResult.ErrorMessage ?? "Execution failed", _clock);
            await _executionRepository.AddAsync(execution, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);
            return ExecuteWorkflowResult.Failed(n8nResult.ErrorMessage ?? "Execution failed");
        }

        // 5. Update execution with n8n execution ID
        execution.MarkRunning(n8nResult.N8nExecutionId!);

        // 6. Persist
        await _executionRepository.AddAsync(execution, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        // 7. Publish domain events
        foreach (var domainEvent in execution.DomainEvents)
        {
            await _publisher.Publish(domainEvent, cancellationToken);
        }
        execution.ClearDomainEvents();

        return ExecuteWorkflowResult.Succeeded(executionId, n8nResult.N8nExecutionId!);
    }
}
