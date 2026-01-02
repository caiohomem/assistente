using AssistenteExecutivo.Application.DTOs;
using AssistenteExecutivo.Application.Json;
using AssistenteExecutivo.Domain.Enums;
using AssistenteExecutivo.Domain.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace AssistenteExecutivo.Infrastructure.Services.N8n;

public sealed class WorkflowSpecValidator : IWorkflowSpecValidator
{
    private readonly ILogger<WorkflowSpecValidator> _logger;
    private readonly HashSet<string> _allowedHosts;
    private readonly JsonSerializerOptions _jsonOptions;

    public WorkflowSpecValidator(
        IConfiguration configuration,
        ILogger<WorkflowSpecValidator> logger)
    {
        _logger = logger;

        // Load allowed hosts from configuration
        var allowedHostsConfig = configuration.GetSection("N8n:AllowedHosts").Get<string[]>() ?? Array.Empty<string>();
        _allowedHosts = new HashSet<string>(allowedHostsConfig, StringComparer.OrdinalIgnoreCase)
        {
            "api.openai.com",
            "graph.microsoft.com",
            "api.twilio.com",
            "api.sendgrid.com",
            "n8n.assistente.live"
        };

        _jsonOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            PropertyNameCaseInsensitive = true,
            Converters = { new CaseInsensitiveJsonStringEnumConverter() }
        };
    }

    public Task<WorkflowSpecValidationResult> ValidateAsync(
        string specJson,
        CancellationToken cancellationToken = default)
    {
        var errors = new List<string>();
        var warnings = new List<string>();

        try
        {
            _logger.LogDebug("Validating workflow spec JSON");

            // Try to parse the JSON
            WorkflowSpecDto? spec;
            try
            {
                spec = JsonSerializer.Deserialize<WorkflowSpecDto>(specJson, _jsonOptions);
            }
            catch (JsonException ex)
            {
                return Task.FromResult(WorkflowSpecValidationResult.Failure($"Invalid JSON: {ex.Message}"));
            }

            if (spec == null)
            {
                return Task.FromResult(WorkflowSpecValidationResult.Failure("Workflow spec is null"));
            }

            // Validate required fields
            if (string.IsNullOrWhiteSpace(spec.Name))
            {
                errors.Add("Workflow name is required");
            }

            if (spec.Steps == null || spec.Steps.Count == 0)
            {
                errors.Add("Workflow must have at least one step");
            }

            // Validate trigger
            ValidateTrigger(spec.Trigger, errors, warnings);

            // Validate steps
            if (spec.Steps != null)
            {
                var stepIds = new HashSet<string>();
                foreach (var step in spec.Steps)
                {
                    ValidateStep(step, stepIds, spec.Steps, errors, warnings);
                }
            }

            // Validate variables
            if (spec.Variables != null)
            {
                foreach (var (name, variable) in spec.Variables)
                {
                    if (string.IsNullOrWhiteSpace(name))
                    {
                        errors.Add("Variable name cannot be empty");
                    }

                    if (variable.Required && variable.DefaultValue == null)
                    {
                        warnings.Add($"Required variable '{name}' has no default value");
                    }
                }
            }

            if (errors.Count > 0)
            {
                _logger.LogWarning("Workflow spec validation failed with {ErrorCount} errors", errors.Count);
                return Task.FromResult(new WorkflowSpecValidationResult
                {
                    IsValid = false,
                    Errors = errors,
                    Warnings = warnings
                });
            }

            _logger.LogDebug("Workflow spec validation passed");
            return Task.FromResult(new WorkflowSpecValidationResult
            {
                IsValid = true,
                Warnings = warnings
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error during workflow spec validation");
            return Task.FromResult(WorkflowSpecValidationResult.Failure($"Validation error: {ex.Message}"));
        }
    }

    private void ValidateTrigger(TriggerSpecDto? trigger, List<string> errors, List<string> warnings)
    {
        if (trigger == null)
        {
            errors.Add("Trigger is required");
            return;
        }

        switch (trigger.Type)
        {
            case TriggerType.Scheduled:
                if (string.IsNullOrWhiteSpace(trigger.CronExpression))
                {
                    errors.Add("Scheduled trigger requires a cron expression");
                }
                else if (!IsValidCronExpression(trigger.CronExpression))
                {
                    errors.Add($"Invalid cron expression: {trigger.CronExpression}");
                }
                break;

            case TriggerType.EventBased:
                if (string.IsNullOrWhiteSpace(trigger.EventName))
                {
                    errors.Add("Event-based trigger requires an event name");
                }
                break;

            case TriggerType.Webhook:
                if (string.IsNullOrWhiteSpace(trigger.EventName))
                {
                    errors.Add("Webhook trigger requires a path");
                }
                break;

            case TriggerType.Manual:
                errors.Add("Manual trigger is not supported. Use Webhook, Scheduled, or EventBased.");
                break;
        }
    }

    private void ValidateStep(
        StepSpecDto step,
        HashSet<string> stepIds,
        List<StepSpecDto> allSteps,
        List<string> errors,
        List<string> warnings)
    {
        // Validate step ID uniqueness
        if (string.IsNullOrWhiteSpace(step.Id))
        {
            errors.Add("Step ID is required");
        }
        else if (!stepIds.Add(step.Id))
        {
            errors.Add($"Duplicate step ID: {step.Id}");
        }

        // Validate step name
        if (string.IsNullOrWhiteSpace(step.Name))
        {
            errors.Add($"Step '{step.Id}' requires a name");
        }

        // Validate based on step type
        if (step.Type == StepType.Action)
        {
            ValidateAction(step, errors, warnings);
        }
        else if (step.Type == StepType.Condition)
        {
            ValidateCondition(step, errors);
        }

        // Validate step references
        ValidateStepReferences(step.OnSuccess, stepIds, allSteps, "OnSuccess", step.Id, errors);
        ValidateStepReferences(step.OnFailure, stepIds, allSteps, "OnFailure", step.Id, errors);

        if (step.Condition?.TrueBranch != null)
        {
            ValidateStepReferences(step.Condition.TrueBranch, stepIds, allSteps, "TrueBranch", step.Id, errors);
        }

        if (step.Condition?.FalseBranch != null)
        {
            ValidateStepReferences(step.Condition.FalseBranch, stepIds, allSteps, "FalseBranch", step.Id, errors);
        }
    }

    private void ValidateAction(StepSpecDto step, List<string> errors, List<string> warnings)
    {
        if (step.Action == null)
        {
            errors.Add($"Action step '{step.Id}' requires an Action property");
            return;
        }

        var action = step.Action;

        // Validate action type
        if (!Enum.IsDefined(typeof(ActionType), action.ActionType))
        {
            errors.Add($"Invalid action type for step '{step.Id}'");
            return;
        }

        // Validate HTTP request URLs against allowlist
        if (action.ActionType == ActionType.HttpRequest)
        {
            if (action.Parameters.TryGetValue("url", out var urlObj) && urlObj is string url)
            {
                ValidateUrl(url, step.Id, errors);
            }
            else
            {
                errors.Add($"HTTP request action '{step.Id}' requires a URL parameter");
            }
        }

        // Validate email action
        if (action.ActionType == ActionType.SendEmail)
        {
            if (!action.Parameters.ContainsKey("to"))
            {
                errors.Add($"Email action '{step.Id}' requires a 'to' parameter");
            }
            if (!action.Parameters.ContainsKey("subject"))
            {
                warnings.Add($"Email action '{step.Id}' has no subject");
            }
        }

        // Check for approval requirement on sensitive actions
        if (IsSensitiveAction(action.ActionType) && !step.ApprovalRequired)
        {
            warnings.Add($"Action '{step.Id}' ({action.ActionType}) is sensitive but does not require approval");
        }
    }

    private void ValidateCondition(StepSpecDto step, List<string> errors)
    {
        if (step.Condition == null)
        {
            errors.Add($"Condition step '{step.Id}' requires a Condition property");
            return;
        }

        var condition = step.Condition;

        if (string.IsNullOrWhiteSpace(condition.LeftOperand))
        {
            errors.Add($"Condition '{step.Id}' requires a left operand");
        }

        if (!Enum.IsDefined(typeof(ConditionType), condition.ConditionType))
        {
            errors.Add($"Invalid condition type for step '{step.Id}'");
        }

        // Some condition types require a right operand
        var requiresRightOperand = condition.ConditionType != ConditionType.IsEmpty &&
                                   condition.ConditionType != ConditionType.IsNotEmpty;

        if (requiresRightOperand && condition.RightOperand == null)
        {
            errors.Add($"Condition '{step.Id}' of type {condition.ConditionType} requires a right operand");
        }
    }

    private void ValidateStepReferences(
        List<string>? references,
        HashSet<string> knownIds,
        List<StepSpecDto> allSteps,
        string referenceType,
        string stepId,
        List<string> errors)
    {
        if (references == null) return;

        var allStepIds = allSteps.Select(s => s.Id).ToHashSet();

        foreach (var refId in references)
        {
            if (!allStepIds.Contains(refId))
            {
                errors.Add($"Step '{stepId}' {referenceType} references non-existent step '{refId}'");
            }
        }
    }

    private void ValidateUrl(string url, string stepId, List<string> errors)
    {
        // Skip template expressions
        if (url.Contains("{{") && url.Contains("}}"))
        {
            return;
        }

        if (!Uri.TryCreate(url, UriKind.Absolute, out var uri))
        {
            errors.Add($"Invalid URL in step '{stepId}': {url}");
            return;
        }

        if (uri.Scheme != "https")
        {
            errors.Add($"URL in step '{stepId}' must use HTTPS");
            return;
        }

        if (!_allowedHosts.Contains(uri.Host) && !uri.Host.EndsWith(".assistenteexecutivo.com"))
        {
            errors.Add($"URL host '{uri.Host}' in step '{stepId}' is not in the allowlist");
        }
    }

    private static bool IsValidCronExpression(string cronExpression)
    {
        // Basic validation - 5 or 6 fields separated by spaces
        var parts = cronExpression.Trim().Split(' ', StringSplitOptions.RemoveEmptyEntries);
        return parts.Length >= 5 && parts.Length <= 6;
    }

    private static bool IsSensitiveAction(ActionType actionType)
    {
        return actionType switch
        {
            ActionType.SendEmail => true,
            ActionType.SendWhatsApp => true,
            ActionType.ScheduleMeeting => true,
            ActionType.UpdateContact => true,
            ActionType.HttpRequest => true,
            _ => false
        };
    }
}
