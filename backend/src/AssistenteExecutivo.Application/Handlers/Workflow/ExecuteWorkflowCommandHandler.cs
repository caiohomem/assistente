using AssistenteExecutivo.Application.Commands.Workflow;
using AssistenteExecutivo.Application.Interfaces;
using AssistenteExecutivo.Domain.Entities;
using AssistenteExecutivo.Domain.Enums;
using AssistenteExecutivo.Domain.Interfaces;
using MediatR;
using System.Text.Json;

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

        var idempotencyKey = executionId.ToString();
        execution.SetIdempotencyKey(idempotencyKey);
        await _executionRepository.AddAsync(execution, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        // 4. Execute via Flow Runner
        var runResult = await _n8nProvider.RunWorkflowAsync(
            workflow.N8nWorkflowId,
            request.InputJson,
            workflow.OwnerUserId,
            request.OwnerUserId.ToString(),
            waitForCompletion: true,
            timeoutSeconds: 300,
            idempotencyKey: idempotencyKey,
            cancellationToken: cancellationToken);

        if (!runResult.IsSuccess)
        {
            execution.Fail(runResult.ErrorMessage ?? "Execution failed", _clock);
            await _executionRepository.UpdateAsync(execution, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);
            return ExecuteWorkflowResult.Failed(runResult.ErrorMessage ?? "Execution failed");
        }

        if (!string.IsNullOrWhiteSpace(runResult.ExecutionId))
        {
            execution.SetN8nExecutionId(runResult.ExecutionId);
        }

        var normalizedStatus = (runResult.Status ?? "Running").Trim();
        if (normalizedStatus.Equals("success", StringComparison.OrdinalIgnoreCase) ||
            normalizedStatus.Equals("completed", StringComparison.OrdinalIgnoreCase))
        {
            var outputJson = runResult.Result != null ? JsonSerializer.Serialize(runResult.Result) : null;
            execution.Complete(outputJson, _clock);
        }
        else if (normalizedStatus.Equals("failed", StringComparison.OrdinalIgnoreCase) ||
                 normalizedStatus.Equals("error", StringComparison.OrdinalIgnoreCase) ||
                 normalizedStatus.Equals("timeout", StringComparison.OrdinalIgnoreCase))
        {
            var errorMessage = runResult.ErrorMessage;
            if (string.IsNullOrWhiteSpace(errorMessage) && runResult.Error != null)
            {
                errorMessage = runResult.Error is string text
                    ? text
                    : JsonSerializer.Serialize(runResult.Error);
            }
            execution.Fail(errorMessage ?? "Execution failed", _clock);
        }
        else if (normalizedStatus.Equals("waitingapproval", StringComparison.OrdinalIgnoreCase))
        {
            execution.UpdateStatus(WorkflowExecutionStatus.WaitingApproval);
        }
        else
        {
            execution.UpdateStatus(WorkflowExecutionStatus.Running);
        }

        // 5. Persist
        await _executionRepository.UpdateAsync(execution, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        // 6. Publish domain events
        foreach (var domainEvent in execution.DomainEvents)
        {
            await _publisher.Publish(domainEvent, cancellationToken);
        }
        execution.ClearDomainEvents();

        return ExecuteWorkflowResult.Succeeded(
            executionId,
            runResult.ExecutionId ?? string.Empty);
    }
}
