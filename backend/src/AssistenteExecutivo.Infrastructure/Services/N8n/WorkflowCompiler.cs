using AssistenteExecutivo.Application.DTOs;
using AssistenteExecutivo.Domain.Enums;
using AssistenteExecutivo.Domain.Interfaces;
using Microsoft.Extensions.Logging;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.Json.Serialization;

namespace AssistenteExecutivo.Infrastructure.Services.N8n;

public sealed class WorkflowCompiler : IWorkflowCompiler
{
    private readonly ILogger<WorkflowCompiler> _logger;
    private readonly JsonSerializerOptions _jsonOptions;

    public WorkflowCompiler(ILogger<WorkflowCompiler> logger)
    {
        _logger = logger;
        _jsonOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
            WriteIndented = false,
            Converters = { new JsonStringEnumConverter() }
        };
    }

    public Task<WorkflowCompilationResult> CompileAsync(
        string name,
        string specJson,
        Guid ownerUserId,
        CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Compiling workflow spec: {Name}", name);

            var spec = JsonSerializer.Deserialize<WorkflowSpecDto>(specJson, _jsonOptions);
            if (spec == null)
            {
                return Task.FromResult(WorkflowCompilationResult.Failed("Failed to parse workflow spec JSON"));
            }

            var n8nWorkflow = CompileToN8n(name, spec, ownerUserId);
            var compiledJson = JsonSerializer.Serialize(n8nWorkflow, _jsonOptions);

            _logger.LogInformation("Successfully compiled workflow with {StepCount} steps", spec.Steps.Count);
            return Task.FromResult(WorkflowCompilationResult.Succeeded(compiledJson));
        }
        catch (JsonException ex)
        {
            _logger.LogError(ex, "JSON parsing error while compiling workflow");
            return Task.FromResult(WorkflowCompilationResult.Failed($"Invalid JSON: {ex.Message}"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error compiling workflow");
            return Task.FromResult(WorkflowCompilationResult.Failed(ex.Message));
        }
    }

    private N8nWorkflowDefinition CompileToN8n(string name, WorkflowSpecDto spec, Guid ownerUserId)
    {
        var nodes = new List<N8nNode>();
        var connections = new Dictionary<string, N8nConnectionSet>();

        // Create trigger node
        var triggerNode = CreateTriggerNode(spec.Trigger);
        nodes.Add(triggerNode);

        // Create step nodes
        var stepNodes = new Dictionary<string, N8nNode>();
        var positionY = 200;

        foreach (var step in spec.Steps)
        {
            var node = CreateStepNode(step, positionY);
            nodes.Add(node);
            stepNodes[step.Id] = node;
            positionY += 150;
        }

        // Create connections
        if (spec.Steps.Count > 0)
        {
            // Connect trigger to first step
            var firstStepId = spec.Steps[0].Id;
            if (stepNodes.TryGetValue(firstStepId, out var firstNode))
            {
                connections[triggerNode.Name] = new N8nConnectionSet
                {
                    Main = new List<List<N8nConnection>>
                    {
                        new() { new N8nConnection { Node = firstNode.Name, Type = "main", Index = 0 } }
                    }
                };
            }

            // Connect steps based on OnSuccess/OnFailure
            foreach (var step in spec.Steps)
            {
                if (!stepNodes.TryGetValue(step.Id, out var sourceNode)) continue;

                var mainConnections = new List<List<N8nConnection>>();

                // OnSuccess connections
                var successConnections = new List<N8nConnection>();
                if (step.OnSuccess != null)
                {
                    foreach (var targetId in step.OnSuccess)
                    {
                        if (stepNodes.TryGetValue(targetId, out var targetNode))
                        {
                            successConnections.Add(new N8nConnection
                            {
                                Node = targetNode.Name,
                                Type = "main",
                                Index = 0
                            });
                        }
                    }
                }
                mainConnections.Add(successConnections);

                // OnFailure connections (for conditions, output 1)
                if (step.Type == StepType.Condition && step.Condition?.FalseBranch != null)
                {
                    var failureConnections = new List<N8nConnection>();
                    foreach (var targetId in step.Condition.FalseBranch)
                    {
                        if (stepNodes.TryGetValue(targetId, out var targetNode))
                        {
                            failureConnections.Add(new N8nConnection
                            {
                                Node = targetNode.Name,
                                Type = "main",
                                Index = 0
                            });
                        }
                    }
                    mainConnections.Add(failureConnections);
                }

                if (mainConnections.Any(c => c.Count > 0))
                {
                    connections[sourceNode.Name] = new N8nConnectionSet { Main = mainConnections };
                }
            }
        }

        return new N8nWorkflowDefinition
        {
            Name = name,
            Active = false,
            Nodes = nodes,
            Connections = connections,
            Tags = new List<N8nTag>
            {
                new() { Name = $"owner:{ownerUserId}" },
                new() { Name = "assistente-executivo" }
            },
            Settings = new N8nSettings
            {
                ExecutionOrder = "v1"
            }
        };
    }

    private N8nNode CreateTriggerNode(TriggerSpecDto trigger)
    {
        return trigger.Type switch
        {
            TriggerType.Manual => new N8nNode
            {
                Name = "Trigger",
                Type = "n8n-nodes-base.manualTrigger",
                TypeVersion = 1,
                Position = new[] { 250, 300 },
                Parameters = new Dictionary<string, object>()
            },
            TriggerType.Scheduled => new N8nNode
            {
                Name = "Trigger",
                Type = "n8n-nodes-base.scheduleTrigger",
                TypeVersion = 1,
                Position = new[] { 250, 300 },
                Parameters = new Dictionary<string, object>
                {
                    ["rule"] = new
                    {
                        interval = new[]
                        {
                            new { field = "cronExpression", expression = trigger.CronExpression ?? "0 9 * * *" }
                        }
                    }
                }
            },
            TriggerType.EventBased => new N8nNode
            {
                Name = "Trigger",
                Type = "n8n-nodes-base.webhook",
                TypeVersion = 1,
                Position = new[] { 250, 300 },
                Parameters = new Dictionary<string, object>
                {
                    ["path"] = trigger.EventName ?? "webhook",
                    ["httpMethod"] = "POST"
                }
            },
            _ => throw new ArgumentException($"Unknown trigger type: {trigger.Type}")
        };
    }

    private N8nNode CreateStepNode(StepSpecDto step, int positionY)
    {
        if (step.Type == StepType.Condition && step.Condition != null)
        {
            return CreateConditionNode(step, positionY);
        }

        if (step.Action == null)
        {
            throw new ArgumentException($"Step {step.Id} is an action but has no Action property");
        }

        return CreateActionNode(step, positionY);
    }

    private N8nNode CreateConditionNode(StepSpecDto step, int positionY)
    {
        var condition = step.Condition!;

        return new N8nNode
        {
            Name = step.Name,
            Type = "n8n-nodes-base.if",
            TypeVersion = 1,
            Position = new[] { 500, positionY },
            Parameters = new Dictionary<string, object>
            {
                ["conditions"] = new
                {
                    string_ = new[]
                    {
                        new
                        {
                            value1 = condition.LeftOperand,
                            operation = MapConditionType(condition.ConditionType),
                            value2 = condition.RightOperand?.ToString() ?? ""
                        }
                    }
                }
            }
        };
    }

    private N8nNode CreateActionNode(StepSpecDto step, int positionY)
    {
        var action = step.Action!;
        var (nodeType, parameters) = MapActionToN8nNode(action);

        return new N8nNode
        {
            Name = step.Name,
            Type = nodeType,
            TypeVersion = 1,
            Position = new[] { 500, positionY },
            Parameters = parameters
        };
    }

    private (string NodeType, Dictionary<string, object> Parameters) MapActionToN8nNode(ActionSpecDto action)
    {
        return action.ActionType switch
        {
            ActionType.SendEmail => ("n8n-nodes-base.emailSend", new Dictionary<string, object>
            {
                ["fromEmail"] = action.Parameters.GetValueOrDefault("from", ""),
                ["toEmail"] = action.Parameters.GetValueOrDefault("to", ""),
                ["subject"] = action.Parameters.GetValueOrDefault("subject", ""),
                ["text"] = action.Parameters.GetValueOrDefault("body", "")
            }),

            ActionType.HttpRequest => ("n8n-nodes-base.httpRequest", new Dictionary<string, object>
            {
                ["url"] = action.Parameters.GetValueOrDefault("url", ""),
                ["method"] = action.Parameters.GetValueOrDefault("method", "GET"),
                ["sendBody"] = action.Parameters.ContainsKey("body"),
                ["bodyContentType"] = "json",
                ["body"] = action.Parameters.GetValueOrDefault("body", "")
            }),

            ActionType.Wait => ("n8n-nodes-base.wait", new Dictionary<string, object>
            {
                ["amount"] = action.Parameters.GetValueOrDefault("seconds", 5)
            }),

            ActionType.SetVariable => ("n8n-nodes-base.set", new Dictionary<string, object>
            {
                ["values"] = new
                {
                    string_ = new[]
                    {
                        new
                        {
                            name = action.Parameters.GetValueOrDefault("name", "variable"),
                            value = action.Parameters.GetValueOrDefault("value", "")
                        }
                    }
                }
            }),

            ActionType.CreateDocument => ("n8n-nodes-base.httpRequest", new Dictionary<string, object>
            {
                ["url"] = "{{$env.API_BASE_URL}}/api/drafts",
                ["method"] = "POST",
                ["sendBody"] = true,
                ["bodyContentType"] = "json",
                ["body"] = JsonSerializer.Serialize(action.Parameters, _jsonOptions)
            }),

            ActionType.SendWhatsApp => ("n8n-nodes-base.httpRequest", new Dictionary<string, object>
            {
                ["url"] = "{{$env.WHATSAPP_API_URL}}/messages",
                ["method"] = "POST",
                ["sendBody"] = true,
                ["bodyContentType"] = "json",
                ["body"] = JsonSerializer.Serialize(new
                {
                    to = action.Parameters.GetValueOrDefault("to", ""),
                    message = action.Parameters.GetValueOrDefault("message", "")
                }, _jsonOptions)
            }),

            ActionType.ScheduleMeeting => ("n8n-nodes-base.googleCalendar", new Dictionary<string, object>
            {
                ["operation"] = "create",
                ["calendar"] = "primary",
                ["summary"] = action.Parameters.GetValueOrDefault("title", ""),
                ["start"] = action.Parameters.GetValueOrDefault("startTime", ""),
                ["end"] = action.Parameters.GetValueOrDefault("endTime", "")
            }),

            ActionType.CreateReminder => ("n8n-nodes-base.httpRequest", new Dictionary<string, object>
            {
                ["url"] = "{{$env.API_BASE_URL}}/api/reminders",
                ["method"] = "POST",
                ["sendBody"] = true,
                ["bodyContentType"] = "json",
                ["body"] = JsonSerializer.Serialize(action.Parameters, _jsonOptions)
            }),

            ActionType.UpdateContact => ("n8n-nodes-base.httpRequest", new Dictionary<string, object>
            {
                ["url"] = $"{{{{$env.API_BASE_URL}}}}/api/contacts/{action.Parameters.GetValueOrDefault("contactId", "")}",
                ["method"] = "PUT",
                ["sendBody"] = true,
                ["bodyContentType"] = "json",
                ["body"] = JsonSerializer.Serialize(action.Parameters, _jsonOptions)
            }),

            ActionType.CreateNote => ("n8n-nodes-base.httpRequest", new Dictionary<string, object>
            {
                ["url"] = "{{$env.API_BASE_URL}}/api/notes",
                ["method"] = "POST",
                ["sendBody"] = true,
                ["bodyContentType"] = "json",
                ["body"] = JsonSerializer.Serialize(action.Parameters, _jsonOptions)
            }),

            _ => throw new ArgumentException($"Unknown action type: {action.ActionType}")
        };
    }

    private string MapConditionType(ConditionType conditionType)
    {
        return conditionType switch
        {
            ConditionType.Equals => "equals",
            ConditionType.NotEquals => "notEquals",
            ConditionType.Contains => "contains",
            ConditionType.GreaterThan => "larger",
            ConditionType.LessThan => "smaller",
            ConditionType.IsEmpty => "isEmpty",
            ConditionType.IsNotEmpty => "isNotEmpty",
            _ => "equals"
        };
    }
}

// n8n workflow model classes
public class N8nWorkflowDefinition
{
    public string Name { get; set; } = string.Empty;
    public bool Active { get; set; }
    public List<N8nNode> Nodes { get; set; } = new();
    public Dictionary<string, N8nConnectionSet> Connections { get; set; } = new();
    public List<N8nTag> Tags { get; set; } = new();
    public N8nSettings? Settings { get; set; }
}

public class N8nNode
{
    public string Name { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty;
    public int TypeVersion { get; set; } = 1;
    public int[] Position { get; set; } = new[] { 0, 0 };
    public Dictionary<string, object> Parameters { get; set; } = new();
}

public class N8nConnectionSet
{
    public List<List<N8nConnection>> Main { get; set; } = new();
}

public class N8nConnection
{
    public string Node { get; set; } = string.Empty;
    public string Type { get; set; } = "main";
    public int Index { get; set; }
}

public class N8nTag
{
    public string Name { get; set; } = string.Empty;
}

public class N8nSettings
{
    public string ExecutionOrder { get; set; } = "v1";
}
