using AssistenteExecutivo.Application.Commands.Workflow;
using AssistenteExecutivo.Application.DTOs;
using AssistenteExecutivo.Application.Interfaces;
using AssistenteExecutivo.Domain.Interfaces;
using AssistenteExecutivo.Domain.ValueObjects;
using MediatR;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace AssistenteExecutivo.Application.Handlers.Workflow;

public class CreateWorkflowFromSpecCommandHandler : IRequestHandler<CreateWorkflowFromSpecCommand, CreateWorkflowResult>
{
    private readonly IWorkflowRepository _workflowRepository;
    private readonly IWorkflowSpecValidator _specValidator;
    private readonly IWorkflowCompiler _compiler;
    private readonly IN8nProvider _n8nProvider;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IClock _clock;
    private readonly IPublisher _publisher;
    private readonly ILogger<CreateWorkflowFromSpecCommandHandler> _logger;

    public CreateWorkflowFromSpecCommandHandler(
        IWorkflowRepository workflowRepository,
        IWorkflowSpecValidator specValidator,
        IWorkflowCompiler compiler,
        IN8nProvider n8nProvider,
        IUnitOfWork unitOfWork,
        IClock clock,
        IPublisher publisher,
        ILogger<CreateWorkflowFromSpecCommandHandler> logger)
    {
        _workflowRepository = workflowRepository;
        _specValidator = specValidator;
        _compiler = compiler;
        _n8nProvider = n8nProvider;
        _unitOfWork = unitOfWork;
        _clock = clock;
        _publisher = publisher;
        _logger = logger;
    }

    public async Task<CreateWorkflowResult> Handle(CreateWorkflowFromSpecCommand request, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Creating workflow from spec for user {UserId}", request.OwnerUserId);

        // 1. Validate the spec locally first
        var validationResult = await _specValidator.ValidateAsync(request.SpecJson, cancellationToken);
        if (!validationResult.IsValid)
        {
            _logger.LogWarning("Workflow spec validation failed: {Errors}", string.Join(", ", validationResult.Errors));
            return CreateWorkflowResult.Failed(validationResult.Errors);
        }

        // 2. Parse spec to get name and trigger
        var spec = JsonSerializer.Deserialize<WorkflowSpecDto>(request.SpecJson, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        });

        if (spec == null || string.IsNullOrWhiteSpace(spec.Name))
        {
            _logger.LogWarning("Failed to parse workflow spec or name is missing");
            return CreateWorkflowResult.Failed("Failed to parse workflow spec or name is missing");
        }

        // 3. Check for duplicate name
        if (await _workflowRepository.ExistsByNameAndOwnerAsync(spec.Name, request.OwnerUserId, cancellationToken))
        {
            _logger.LogWarning("Workflow with name '{Name}' already exists for user {UserId}", spec.Name, request.OwnerUserId);
            return CreateWorkflowResult.Failed($"A workflow with name '{spec.Name}' already exists");
        }

        // 4. Try to use Flow Builder system workflow (preferred)
        var idempotencyKey = $"workflow-{request.OwnerUserId}-{spec.Name}-{_clock.UtcNow:yyyyMMddHHmmss}";
        var buildResult = await _n8nProvider.BuildWorkflowAsync(
            request.SpecJson,
            request.OwnerUserId,
            request.OwnerUserId.ToString(),
            idempotencyKey,
            cancellationToken);

        string? n8nWorkflowId;
        Guid workflowId;

        if (buildResult.IsSuccess)
        {
            // Flow Builder succeeded - workflow was created in n8n and registered in database
            _logger.LogInformation("Flow Builder created workflow {WorkflowId} with spec {SpecId} v{Version}",
                buildResult.WorkflowId, buildResult.SpecId, buildResult.SpecVersion);

            n8nWorkflowId = buildResult.WorkflowId;
            workflowId = Guid.TryParse(buildResult.SpecId, out var specGuid) ? specGuid : Guid.NewGuid();

            // Activate if requested
            if (request.ActivateImmediately && !string.IsNullOrEmpty(n8nWorkflowId))
            {
                try
                {
                    await _n8nProvider.ActivateWorkflowAsync(n8nWorkflowId, cancellationToken);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to activate workflow, but it was created successfully");
                }
            }

            return CreateWorkflowResult.Succeeded(workflowId, n8nWorkflowId!);
        }

        // 5. Flow Builder failed - fallback to direct API method
        _logger.LogWarning("Flow Builder failed: {Error}. Falling back to direct API.", buildResult.ErrorMessage);

        // Compile spec to n8n format
        var compilationResult = await _compiler.CompileAsync(spec.Name, request.SpecJson, request.OwnerUserId, cancellationToken);
        if (!compilationResult.Success)
        {
            _logger.LogWarning("Workflow compilation failed: {Errors}", string.Join(", ", compilationResult.Errors));
            return CreateWorkflowResult.Failed(compilationResult.Errors);
        }

        // Create workflow in n8n via direct API
        var n8nResult = await _n8nProvider.CreateWorkflowAsync(spec.Name, compilationResult.CompiledJson!, cancellationToken);
        if (!n8nResult.Success)
        {
            _logger.LogError("Failed to create workflow in n8n: {Error}", n8nResult.ErrorMessage);
            return CreateWorkflowResult.Failed(n8nResult.ErrorMessage ?? "Failed to create workflow in n8n");
        }

        n8nWorkflowId = n8nResult.N8nWorkflowId;

        // 6. Create trigger value object
        var trigger = spec.Trigger.Type switch
        {
            Domain.Enums.TriggerType.Manual => WorkflowTrigger.Manual(),
            Domain.Enums.TriggerType.Scheduled => WorkflowTrigger.Scheduled(spec.Trigger.CronExpression ?? "0 9 * * *"),
            Domain.Enums.TriggerType.EventBased => WorkflowTrigger.EventBased(
                spec.Trigger.EventName ?? "webhook",
                spec.Trigger.Config != null ? JsonSerializer.Serialize(spec.Trigger.Config) : null),
            _ => WorkflowTrigger.Manual()
        };

        // 7. Create domain entity
        workflowId = Guid.NewGuid();
        var workflow = Domain.Entities.Workflow.Create(
            workflowId,
            request.OwnerUserId,
            spec.Name,
            request.SpecJson,
            trigger,
            _clock);

        // Set n8n workflow ID
        workflow.SetN8nWorkflowId(n8nWorkflowId!, _clock);

        // Update description if provided
        if (!string.IsNullOrWhiteSpace(spec.Description))
        {
            workflow.UpdateDescription(spec.Description, _clock);
        }

        // 8. Activate if requested
        if (request.ActivateImmediately)
        {
            try
            {
                await _n8nProvider.ActivateWorkflowAsync(n8nWorkflowId!, cancellationToken);
                workflow.Activate(_clock);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to activate workflow");
            }
        }

        // 9. Persist
        await _workflowRepository.AddAsync(workflow, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        // 10. Publish domain events
        foreach (var domainEvent in workflow.DomainEvents)
        {
            await _publisher.Publish(domainEvent, cancellationToken);
        }
        workflow.ClearDomainEvents();

        _logger.LogInformation("Workflow {WorkflowId} created successfully via direct API", workflowId);
        return CreateWorkflowResult.Succeeded(workflowId, n8nWorkflowId!);
    }
}
