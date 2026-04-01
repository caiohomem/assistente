using AssistenteExecutivo.Application.DTOs;
using AssistenteExecutivo.Application.Json;
using AssistenteExecutivo.Domain.Enums;
using AssistenteExecutivo.Domain.Interfaces;
using Microsoft.Extensions.Logging;
using System.Text.Json;
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
            Converters = { new CaseInsensitiveJsonStringEnumConverter() }
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
            TriggerType.Manual => AssignNodeIdentity(new N8nNode
            {
                Name = "Trigger",
                Type = "n8n-nodes-base.manualTrigger",
                TypeVersion = 1,
                Position = new[] { 250, 300 },
                Parameters = new Dictionary<string, object>()
            }),
            TriggerType.Scheduled => AssignNodeIdentity(new N8nNode
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
            }),
            TriggerType.EventBased => AssignNodeIdentity(new N8nNode
            {
                Name = "Trigger",
                Type = "n8n-nodes-base.webhook",
                TypeVersion = 2,
                Position = new[] { 250, 300 },
                Parameters = new Dictionary<string, object>
                {
                    ["path"] = trigger.EventName ?? "webhook",
                    ["httpMethod"] = "POST"
                }
            }, includeWebhookId: true),
            TriggerType.Webhook => AssignNodeIdentity(new N8nNode
            {
                Name = "Trigger",
                Type = "n8n-nodes-base.webhook",
                TypeVersion = 2,
                Position = new[] { 250, 300 },
                Parameters = new Dictionary<string, object>
                {
                    ["path"] = trigger.EventName ?? "workflow",
                    ["httpMethod"] = "POST"
                }
            }, includeWebhookId: true),
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

        return AssignNodeIdentity(new N8nNode
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
        });
    }

    private N8nNode CreateActionNode(StepSpecDto step, int positionY)
    {
        var action = step.Action!;
        var (nodeType, typeVersion, parameters, credentials) = MapActionToN8nNode(action);

        return AssignNodeIdentity(new N8nNode
        {
            Name = step.Name,
            Type = nodeType,
            TypeVersion = typeVersion,
            Position = new[] { 500, positionY },
            Parameters = parameters,
            Credentials = credentials
        });
    }

    private (string NodeType, double TypeVersion, Dictionary<string, object> Parameters, Dictionary<string, object>? Credentials) MapActionToN8nNode(ActionSpecDto action)
    {
        return action.ActionType switch
        {
            ActionType.SendEmail => CreateMailjetEmailAction(action),

            ActionType.HttpRequest => ("n8n-nodes-base.httpRequest", 1, new Dictionary<string, object>
            {
                ["url"] = action.Parameters.GetValueOrDefault("url", ""),
                ["method"] = action.Parameters.GetValueOrDefault("method", "GET"),
                ["sendBody"] = action.Parameters.ContainsKey("body"),
                ["bodyContentType"] = "json",
                ["body"] = action.Parameters.GetValueOrDefault("body", "")
            }, null),

            ActionType.Wait => ("n8n-nodes-base.wait", 1, new Dictionary<string, object>
            {
                ["amount"] = action.Parameters.GetValueOrDefault("seconds", 5)
            }, null),

            ActionType.SetVariable => ("n8n-nodes-base.set", 1, new Dictionary<string, object>
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
            }, null),

            ActionType.CreateDocument => ("n8n-nodes-base.httpRequest", 1, new Dictionary<string, object>
            {
                ["url"] = "{{$env.API_BASE_URL}}/api/drafts",
                ["method"] = "POST",
                ["sendBody"] = true,
                ["bodyContentType"] = "json",
                ["body"] = JsonSerializer.Serialize(action.Parameters, _jsonOptions)
            }, null),

            ActionType.SendWhatsApp => ("n8n-nodes-base.httpRequest", 1, new Dictionary<string, object>
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
            }, null),

            ActionType.ScheduleMeeting => ("n8n-nodes-base.googleCalendar", 1, new Dictionary<string, object>
            {
                ["operation"] = "create",
                ["calendar"] = "primary",
                ["summary"] = action.Parameters.GetValueOrDefault("title", ""),
                ["start"] = action.Parameters.GetValueOrDefault("startTime", ""),
                ["end"] = action.Parameters.GetValueOrDefault("endTime", "")
            }, null),

            ActionType.CreateReminder => ("n8n-nodes-base.httpRequest", 1, new Dictionary<string, object>
            {
                ["url"] = "{{$env.API_BASE_URL}}/api/reminders",
                ["method"] = "POST",
                ["sendBody"] = true,
                ["bodyContentType"] = "json",
                ["body"] = JsonSerializer.Serialize(action.Parameters, _jsonOptions)
            }, null),

            ActionType.UpdateContact => ("n8n-nodes-base.httpRequest", 1, new Dictionary<string, object>
            {
                ["url"] = $"{{{{$env.API_BASE_URL}}}}/api/contacts/{action.Parameters.GetValueOrDefault("contactId", "")}",
                ["method"] = "PUT",
                ["sendBody"] = true,
                ["bodyContentType"] = "json",
                ["body"] = JsonSerializer.Serialize(action.Parameters, _jsonOptions)
            }, null),

            ActionType.CreateNote => ("n8n-nodes-base.httpRequest", 1, new Dictionary<string, object>
            {
                ["url"] = "{{$env.API_BASE_URL}}/api/notes",
                ["method"] = "POST",
                ["sendBody"] = true,
                ["bodyContentType"] = "json",
                ["body"] = JsonSerializer.Serialize(action.Parameters, _jsonOptions)
            }, null),

            _ => throw new ArgumentException($"Unknown action type: {action.ActionType}")
        };
    }

    private (string NodeType, double TypeVersion, Dictionary<string, object> Parameters, Dictionary<string, object>? Credentials) CreateMailjetEmailAction(ActionSpecDto action)
    {
        var parameters = new Dictionary<string, object>
        {
            ["fromEmail"] = GetStringParameter(action.Parameters, "", "fromEmail", "from"),
            ["toEmail"] = GetStringParameter(action.Parameters, "", "toEmail", "to"),
            ["subject"] = GetStringParameter(action.Parameters, "", "subject"),
            ["text"] = GetStringParameter(action.Parameters, "", "text", "body"),
            ["additionalFields"] = action.Parameters.GetValueOrDefault("additionalFields", new Dictionary<string, object>())
        };

        var credentials = BuildMailjetCredentials(action.Parameters);

        return ("n8n-nodes-base.mailjet", 2.1, parameters, credentials);
    }

    private static Dictionary<string, object>? BuildMailjetCredentials(Dictionary<string, object> parameters)
    {
        if (TryConvertToDictionary(parameters.GetValueOrDefault("credentials"), out var directCredentials))
        {
            return directCredentials;
        }

        var credentialId = GetStringParameter(parameters, "", "credentialId", "mailjetCredentialId");
        var credentialName = GetStringParameter(parameters, "Mailjet Email account", "credentialName", "mailjetCredentialName");

        if (string.IsNullOrWhiteSpace(credentialId) && string.IsNullOrWhiteSpace(credentialName))
        {
            return null;
        }

        return new Dictionary<string, object>
        {
            ["mailjetEmailApi"] = new Dictionary<string, object>
            {
                ["id"] = credentialId,
                ["name"] = credentialName
            }
        };
    }

    private static bool TryConvertToDictionary(object? value, out Dictionary<string, object>? dictionary)
    {
        dictionary = null;

        if (value is Dictionary<string, object> dict)
        {
            dictionary = dict;
            return true;
        }

        if (value is JsonElement element && element.ValueKind == JsonValueKind.Object)
        {
            var deserialized = JsonSerializer.Deserialize<Dictionary<string, object>>(element.GetRawText());
            if (deserialized != null)
            {
                dictionary = deserialized;
                return true;
            }
        }

        return false;
    }

    private static string GetStringParameter(Dictionary<string, object> parameters, string defaultValue, params string[] keys)
    {
        foreach (var key in keys)
        {
            if (parameters.TryGetValue(key, out var value))
            {
                var stringValue = ConvertToString(value);
                if (!string.IsNullOrEmpty(stringValue))
                {
                    return stringValue;
                }
            }
        }

        return defaultValue;
    }

    private static string ConvertToString(object? value)
    {
        if (value == null)
        {
            return string.Empty;
        }

        if (value is string str)
        {
            return str;
        }

        if (value is JsonElement element)
        {
            return element.ValueKind switch
            {
                JsonValueKind.String => element.GetString() ?? string.Empty,
                JsonValueKind.Number => element.ToString(),
                JsonValueKind.True => "true",
                JsonValueKind.False => "false",
                JsonValueKind.Object or JsonValueKind.Array => element.GetRawText(),
                _ => string.Empty
            };
        }

        return value.ToString() ?? string.Empty;
    }

    private static N8nNode AssignNodeIdentity(N8nNode node, bool includeWebhookId = false)
    {
        node.Id = Guid.NewGuid().ToString();
        if (includeWebhookId)
        {
            node.WebhookId = Guid.NewGuid().ToString();
        }
        return node;
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
    public double TypeVersion { get; set; } = 1;
    public int[] Position { get; set; } = new[] { 0, 0 };
    public Dictionary<string, object> Parameters { get; set; } = new();
    public Dictionary<string, object>? Credentials { get; set; }
    public string Id { get; set; } = string.Empty;
    public string? WebhookId { get; set; }
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
