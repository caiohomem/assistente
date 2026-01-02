using AssistenteExecutivo.Application.Commands.Workflow;
using AssistenteExecutivo.Application.Interfaces;
using AssistenteExecutivo.Application.DTOs;
using AssistenteExecutivo.Domain.Entities;
using AssistenteExecutivo.Domain.Enums;
using AssistenteExecutivo.Domain.Interfaces;
using AssistenteExecutivo.Domain.ValueObjects;
using MediatR;
using Microsoft.Extensions.Logging;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace AssistenteExecutivo.Application.Handlers.Workflow;

public class SaveWorkflowSpecCommandHandler : IRequestHandler<SaveWorkflowSpecCommand, SaveWorkflowSpecResult>
{
    private readonly IWorkflowRepository _workflowRepository;
    private readonly IWorkflowSpecValidator _validator;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IClock _clock;
    private readonly ILogger<SaveWorkflowSpecCommandHandler> _logger;

    public SaveWorkflowSpecCommandHandler(
        IWorkflowRepository workflowRepository,
        IWorkflowSpecValidator validator,
        IUnitOfWork unitOfWork,
        IClock clock,
        ILogger<SaveWorkflowSpecCommandHandler> logger)
    {
        _workflowRepository = workflowRepository;
        _validator = validator;
        _unitOfWork = unitOfWork;
        _clock = clock;
        _logger = logger;
    }

    public async Task<SaveWorkflowSpecResult> Handle(SaveWorkflowSpecCommand request, CancellationToken cancellationToken)
    {
        try
        {
            // Validate spec
            var validationResult = await _validator.ValidateAsync(request.SpecJson, cancellationToken);
            if (!validationResult.IsValid)
            {
                return SaveWorkflowSpecResult.Failed(string.Join("; ", validationResult.Errors));
            }

            // Parse tenant ID
            if (!Guid.TryParse(request.TenantId, out var tenantId))
            {
                tenantId = Guid.Empty;
            }

            // Check for existing spec with same idempotency key
            if (!string.IsNullOrEmpty(request.IdempotencyKey))
            {
                var existing = await _workflowRepository.GetByIdempotencyKeyAsync(request.IdempotencyKey, cancellationToken);
                if (existing != null)
                {
                    _logger.LogInformation("Returning existing workflow for idempotency key: {Key}", request.IdempotencyKey);
                    return SaveWorkflowSpecResult.Succeeded(existing.WorkflowId, existing.SpecVersion);
                }
            }

            // Parse spec to get trigger
            var spec = JsonSerializer.Deserialize<WorkflowSpecDto>(request.SpecJson, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true,
                Converters = { new JsonStringEnumConverter() }
            });

            if (spec == null)
            {
                return SaveWorkflowSpecResult.Failed("Failed to parse workflow spec");
            }

            if (spec.Trigger.Type == TriggerType.Manual)
            {
                return SaveWorkflowSpecResult.Failed("Manual trigger is not supported. Use Webhook, Scheduled, or EventBased.");
            }

            var trigger = spec.Trigger.Type switch
            {
                TriggerType.Scheduled => WorkflowTrigger.Scheduled(spec.Trigger.CronExpression ?? "0 9 * * *"),
                TriggerType.EventBased => WorkflowTrigger.EventBased(
                    spec.Trigger.EventName ?? "webhook",
                    spec.Trigger.Config != null ? JsonSerializer.Serialize(spec.Trigger.Config) : null),
                TriggerType.Webhook => WorkflowTrigger.Webhook(
                    spec.Trigger.EventName ?? "workflow",
                    spec.Trigger.Config != null ? JsonSerializer.Serialize(spec.Trigger.Config) : null),
                _ => throw new ArgumentException($"Unsupported trigger type: {spec.Trigger.Type}")
            };

            // Create workflow entity
            var workflow = new Domain.Entities.Workflow(
                name: request.Name,
                ownerUserId: tenantId,
                specJson: request.SpecJson,
                trigger: trigger,
                clock: _clock);

            workflow.SetIdempotencyKey(request.IdempotencyKey);

            // Save
            await _workflowRepository.AddAsync(workflow, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("Saved workflow spec: {WorkflowId}, version {Version}",
                workflow.WorkflowId, workflow.SpecVersion);

            return SaveWorkflowSpecResult.Succeeded(workflow.WorkflowId, workflow.SpecVersion);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error saving workflow spec");
            return SaveWorkflowSpecResult.Failed(ex.Message);
        }
    }
}
