using AssistenteExecutivo.Application.Commands.Workflow;
using AssistenteExecutivo.Application.Interfaces;
using AssistenteExecutivo.Domain.Entities;
using AssistenteExecutivo.Domain.Enums;
using MediatR;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace AssistenteExecutivo.Application.Handlers.Workflow;

public class UpdateWorkflowRunCommandHandler : IRequestHandler<UpdateWorkflowRunCommand, bool>
{
    private readonly IWorkflowExecutionRepository _executionRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<UpdateWorkflowRunCommandHandler> _logger;

    public UpdateWorkflowRunCommandHandler(
        IWorkflowExecutionRepository executionRepository,
        IUnitOfWork unitOfWork,
        ILogger<UpdateWorkflowRunCommandHandler> logger)
    {
        _executionRepository = executionRepository;
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<bool> Handle(UpdateWorkflowRunCommand request, CancellationToken cancellationToken)
    {
        // Try to parse as GUID first
        WorkflowExecution? execution = null;

        if (Guid.TryParse(request.RunId, out var executionId))
        {
            execution = await _executionRepository.GetByIdAsync(executionId, cancellationToken);
        }

        // If not found by GUID, try by idempotency key
        if (execution == null)
        {
            execution = await _executionRepository.GetByIdempotencyKeyAsync(request.RunId, cancellationToken);
        }

        if (execution == null)
        {
            _logger.LogWarning("Execution not found for update: {RunId}", request.RunId);
            return false;
        }

        // Update n8n execution ID
        if (!string.IsNullOrEmpty(request.N8nExecutionId))
        {
            execution.SetN8nExecutionId(request.N8nExecutionId);
        }

        // Parse and set status
        var status = request.Status switch
        {
            "success" or "Success" or "Completed" => WorkflowExecutionStatus.Completed,
            "failed" or "Failed" or "error" => WorkflowExecutionStatus.Failed,
            "timeout" or "Timeout" => WorkflowExecutionStatus.Failed,
            "Accepted" or "Running" => WorkflowExecutionStatus.Running,
            "Cancelled" => WorkflowExecutionStatus.Cancelled,
            "WaitingApproval" => WorkflowExecutionStatus.WaitingApproval,
            _ => execution.Status
        };

        // Serialize result/error
        string? outputJson = null;
        if (request.Result != null)
        {
            outputJson = request.Result is string s ? s : JsonSerializer.Serialize(request.Result);
        }

        string? errorMessage = null;
        if (request.Error != null)
        {
            errorMessage = request.Error is string s ? s : JsonSerializer.Serialize(request.Error);
        }

        // Parse finished time
        DateTime? finishedAt = null;
        if (!string.IsNullOrEmpty(request.FinishedAt))
        {
            if (DateTime.TryParse(request.FinishedAt, out var parsed))
            {
                finishedAt = parsed;
            }
        }

        // Update execution
        if (status == WorkflowExecutionStatus.Completed)
        {
            execution.Complete(outputJson, finishedAt);
        }
        else if (status == WorkflowExecutionStatus.Failed)
        {
            execution.Fail(errorMessage ?? "Execution failed", finishedAt);
        }
        else
        {
            execution.UpdateStatus(status);
        }

        await _executionRepository.UpdateAsync(execution, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Updated workflow run {ExecutionId} to status {Status}",
            execution.ExecutionId, status);

        return true;
    }
}
