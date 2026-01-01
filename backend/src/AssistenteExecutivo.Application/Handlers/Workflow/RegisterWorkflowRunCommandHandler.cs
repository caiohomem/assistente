using AssistenteExecutivo.Application.Commands.Workflow;
using AssistenteExecutivo.Application.Interfaces;
using AssistenteExecutivo.Domain.Entities;
using AssistenteExecutivo.Domain.Enums;
using AssistenteExecutivo.Domain.Interfaces;
using MediatR;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace AssistenteExecutivo.Application.Handlers.Workflow;

public class RegisterWorkflowRunCommandHandler : IRequestHandler<RegisterWorkflowRunCommand, RegisterWorkflowRunResult>
{
    private readonly IWorkflowRepository _workflowRepository;
    private readonly IWorkflowExecutionRepository _executionRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IClock _clock;
    private readonly ILogger<RegisterWorkflowRunCommandHandler> _logger;

    public RegisterWorkflowRunCommandHandler(
        IWorkflowRepository workflowRepository,
        IWorkflowExecutionRepository executionRepository,
        IUnitOfWork unitOfWork,
        IClock clock,
        ILogger<RegisterWorkflowRunCommandHandler> logger)
    {
        _workflowRepository = workflowRepository;
        _executionRepository = executionRepository;
        _unitOfWork = unitOfWork;
        _clock = clock;
        _logger = logger;
    }

    public async Task<RegisterWorkflowRunResult> Handle(RegisterWorkflowRunCommand request, CancellationToken cancellationToken)
    {
        try
        {
            // Check idempotency
            if (!string.IsNullOrEmpty(request.IdempotencyKey))
            {
                var existing = await _executionRepository.GetByIdempotencyKeyAsync(request.IdempotencyKey, cancellationToken);
                if (existing != null)
                {
                    _logger.LogInformation("Returning existing run for idempotency key: {Key}", request.IdempotencyKey);
                    return RegisterWorkflowRunResult.Succeeded(existing.ExecutionId.ToString());
                }
            }

            // Parse IDs
            if (!Guid.TryParse(request.WorkflowId, out var workflowId))
            {
                return RegisterWorkflowRunResult.Failed("Invalid workflow ID");
            }

            if (!Guid.TryParse(request.TenantId, out var tenantId))
            {
                tenantId = Guid.Empty;
            }

            // Get workflow to get spec version
            var workflow = await _workflowRepository.GetByIdAsync(workflowId, cancellationToken);
            if (workflow == null)
            {
                // Workflow might be from n8n directly, create execution anyway
                _logger.LogWarning("Workflow not found: {WorkflowId}, creating execution anyway", workflowId);
            }

            // Parse status
            var status = request.Status switch
            {
                "Running" => WorkflowExecutionStatus.Running,
                "Pending" => WorkflowExecutionStatus.Pending,
                "Accepted" => WorkflowExecutionStatus.Running,
                _ => WorkflowExecutionStatus.Running
            };

            // Parse started time
            DateTime startedAt;
            if (!string.IsNullOrEmpty(request.StartedAt))
            {
                DateTime.TryParse(request.StartedAt, out startedAt);
            }
            else
            {
                startedAt = _clock.UtcNow;
            }

            // Serialize inputs
            string? inputsJson = null;
            if (request.Inputs != null)
            {
                inputsJson = JsonSerializer.Serialize(request.Inputs);
            }

            // Create execution
            var execution = new WorkflowExecution(
                workflowId: workflowId,
                ownerUserId: tenantId,
                specVersionUsed: workflow?.SpecVersion ?? 1,
                inputJson: inputsJson,
                startedAt: startedAt);

            execution.SetIdempotencyKey(request.IdempotencyKey);

            // Save
            await _executionRepository.AddAsync(execution, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("Registered workflow run: {ExecutionId}", execution.ExecutionId);

            return RegisterWorkflowRunResult.Succeeded(execution.ExecutionId.ToString());
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error registering workflow run");
            return RegisterWorkflowRunResult.Failed(ex.Message);
        }
    }
}
