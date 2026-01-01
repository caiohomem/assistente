using AssistenteExecutivo.Domain.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace AssistenteExecutivo.Infrastructure.Services.N8n;

public sealed class N8nProvider : IN8nProvider
{
    private readonly HttpClient _httpClient;
    private readonly HttpClient _webhookClient;
    private readonly ILogger<N8nProvider> _logger;
    private readonly string _webhookBaseUrl;
    private readonly JsonSerializerOptions _jsonOptions;

    public N8nProvider(
        IConfiguration configuration,
        IHttpClientFactory httpClientFactory,
        ILogger<N8nProvider> logger)
    {
        _logger = logger;
        _httpClient = httpClientFactory.CreateClient();
        _webhookClient = httpClientFactory.CreateClient();

        var baseUrl = configuration["N8n:BaseUrl"]
            ?? throw new InvalidOperationException("N8n:BaseUrl not configured");

        var apiKey = configuration["N8n:ApiKey"]
            ?? throw new InvalidOperationException("N8n:ApiKey not configured");

        _webhookBaseUrl = configuration["N8n:WebhookBaseUrl"] ?? $"{baseUrl.TrimEnd('/')}/webhook/";

        // API client
        _httpClient.BaseAddress = new Uri(baseUrl.TrimEnd('/') + "/api/v1/");
        _httpClient.DefaultRequestHeaders.Add("X-N8N-API-KEY", apiKey);
        _httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
        _httpClient.Timeout = TimeSpan.FromMinutes(2);

        // Webhook client
        _webhookClient.BaseAddress = new Uri(_webhookBaseUrl);
        _webhookClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
        _webhookClient.Timeout = TimeSpan.FromMinutes(5);

        _jsonOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
        };
    }

    /// <summary>
    /// Builds a workflow using the Flow Builder system workflow.
    /// This is the preferred method for creating workflows from specs.
    /// </summary>
    public async Task<FlowBuilderResult> BuildWorkflowAsync(
        string specJson,
        Guid tenantId,
        string requestedBy,
        string? idempotencyKey = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Building workflow via Flow Builder for tenant: {TenantId}", tenantId);

            var request = new
            {
                spec = JsonSerializer.Deserialize<object>(specJson, _jsonOptions),
                tenantId = tenantId.ToString(),
                requestedBy,
                mode = "create",
                idempotencyKey = idempotencyKey ?? Guid.NewGuid().ToString()
            };

            var content = new StringContent(
                JsonSerializer.Serialize(request, _jsonOptions),
                Encoding.UTF8,
                "application/json");

            var response = await _webhookClient.PostAsync("system/flows/build", content, cancellationToken);
            var responseBody = await response.Content.ReadAsStringAsync(cancellationToken);

            _logger.LogDebug("Flow Builder response: {Response}", responseBody);

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogError("Flow Builder failed. Status: {Status}, Response: {Response}",
                    response.StatusCode, responseBody);
                return FlowBuilderResult.Failed($"Flow Builder error: {response.StatusCode}");
            }

            var result = JsonSerializer.Deserialize<FlowBuilderResponse>(responseBody, _jsonOptions);
            if (result == null || !result.Success)
            {
                return FlowBuilderResult.Failed(result?.Error ?? "Unknown error from Flow Builder");
            }

            _logger.LogInformation("Flow Builder created workflow: {WorkflowId}, spec: {SpecId} v{Version}",
                result.WorkflowId, result.SpecId, result.SpecVersion);

            return FlowBuilderResult.Succeeded(
                result.WorkflowId!,
                result.SpecId!,
                result.SpecVersion,
                result.Warnings ?? new List<string>());
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error calling Flow Builder");
            return FlowBuilderResult.Failed(ex.Message);
        }
    }

    /// <summary>
    /// Runs a workflow using the Flow Runner system workflow.
    /// This is the preferred method for executing workflows.
    /// </summary>
    public async Task<FlowRunnerResult> RunWorkflowAsync(
        string workflowId,
        string? inputsJson,
        Guid tenantId,
        string requestedBy,
        bool waitForCompletion = true,
        int timeoutSeconds = 300,
        string? idempotencyKey = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Running workflow via Flow Runner: {WorkflowId}", workflowId);

            var inputs = string.IsNullOrWhiteSpace(inputsJson)
                ? new Dictionary<string, object>()
                : JsonSerializer.Deserialize<Dictionary<string, object>>(inputsJson, _jsonOptions);

            var request = new
            {
                workflowId,
                inputs,
                tenantId = tenantId.ToString(),
                requestedBy,
                waitForCompletion,
                timeoutSeconds,
                idempotencyKey = idempotencyKey ?? Guid.NewGuid().ToString()
            };

            var content = new StringContent(
                JsonSerializer.Serialize(request, _jsonOptions),
                Encoding.UTF8,
                "application/json");

            var response = await _webhookClient.PostAsync("system/flows/run", content, cancellationToken);
            var responseBody = await response.Content.ReadAsStringAsync(cancellationToken);

            _logger.LogDebug("Flow Runner response: {Response}", responseBody);

            if (!response.IsSuccessStatusCode && response.StatusCode != System.Net.HttpStatusCode.Accepted)
            {
                _logger.LogError("Flow Runner failed. Status: {Status}, Response: {Response}",
                    response.StatusCode, responseBody);
                return FlowRunnerResult.Failed($"Flow Runner error: {response.StatusCode}");
            }

            var result = JsonSerializer.Deserialize<FlowRunnerResponse>(responseBody, _jsonOptions);
            if (result == null || !result.Success)
            {
                return FlowRunnerResult.Failed(result?.Error ?? "Unknown error from Flow Runner");
            }

            _logger.LogInformation("Flow Runner started execution: {ExecutionId}, status: {Status}",
                result.ExecutionId, result.Status);

            return new FlowRunnerResult
            {
                IsSuccess = true,
                RunId = result.RunId,
                ExecutionId = result.ExecutionId,
                Status = result.Status,
                Result = result.Result,
                Error = result.Error,
                IsAsync = result.Async,
                StartedAt = result.StartedAt,
                FinishedAt = result.FinishedAt
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error calling Flow Runner");
            return FlowRunnerResult.Failed(ex.Message);
        }
    }

    public async Task<N8nWorkflowResult> CreateWorkflowAsync(
        string name,
        string compiledJson,
        CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Creating n8n workflow: {Name}", name);

            var content = new StringContent(compiledJson, Encoding.UTF8, "application/json");
            var response = await _httpClient.PostAsync("workflows", content, cancellationToken);

            var responseBody = await response.Content.ReadAsStringAsync(cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogError("Failed to create n8n workflow. Status: {Status}, Response: {Response}",
                    response.StatusCode, responseBody);
                return N8nWorkflowResult.Failed($"n8n API error: {response.StatusCode}");
            }

            var result = JsonSerializer.Deserialize<N8nWorkflowResponse>(responseBody, _jsonOptions);
            if (result?.Id == null)
            {
                return N8nWorkflowResult.Failed("Failed to parse n8n workflow response");
            }

            _logger.LogInformation("Created n8n workflow with ID: {N8nWorkflowId}", result.Id);
            return N8nWorkflowResult.Succeeded(result.Id);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating n8n workflow");
            return N8nWorkflowResult.Failed(ex.Message);
        }
    }

    public async Task<N8nWorkflowResult> UpdateWorkflowAsync(
        string n8nWorkflowId,
        string name,
        string compiledJson,
        CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Updating n8n workflow: {N8nWorkflowId}", n8nWorkflowId);

            var content = new StringContent(compiledJson, Encoding.UTF8, "application/json");
            var response = await _httpClient.PutAsync($"workflows/{n8nWorkflowId}", content, cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                var responseBody = await response.Content.ReadAsStringAsync(cancellationToken);
                _logger.LogError("Failed to update n8n workflow. Status: {Status}, Response: {Response}",
                    response.StatusCode, responseBody);
                return N8nWorkflowResult.Failed($"n8n API error: {response.StatusCode}");
            }

            return N8nWorkflowResult.Succeeded(n8nWorkflowId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating n8n workflow");
            return N8nWorkflowResult.Failed(ex.Message);
        }
    }

    public async Task ActivateWorkflowAsync(string n8nWorkflowId, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Activating n8n workflow: {N8nWorkflowId}", n8nWorkflowId);

        var content = new StringContent(
            JsonSerializer.Serialize(new { active = true }, _jsonOptions),
            Encoding.UTF8,
            "application/json");

        var response = await _httpClient.PatchAsync($"workflows/{n8nWorkflowId}", content, cancellationToken);
        response.EnsureSuccessStatusCode();
    }

    public async Task DeactivateWorkflowAsync(string n8nWorkflowId, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Deactivating n8n workflow: {N8nWorkflowId}", n8nWorkflowId);

        var content = new StringContent(
            JsonSerializer.Serialize(new { active = false }, _jsonOptions),
            Encoding.UTF8,
            "application/json");

        var response = await _httpClient.PatchAsync($"workflows/{n8nWorkflowId}", content, cancellationToken);
        response.EnsureSuccessStatusCode();
    }

    public async Task<N8nExecutionResult> ExecuteWorkflowAsync(
        string n8nWorkflowId,
        string? inputsJson = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Executing n8n workflow: {N8nWorkflowId}", n8nWorkflowId);

            var requestBody = new { workflowId = n8nWorkflowId };
            if (!string.IsNullOrWhiteSpace(inputsJson))
            {
                var inputs = JsonSerializer.Deserialize<Dictionary<string, object>>(inputsJson, _jsonOptions);
                requestBody = new { workflowId = n8nWorkflowId };
            }

            var content = new StringContent(
                JsonSerializer.Serialize(requestBody, _jsonOptions),
                Encoding.UTF8,
                "application/json");

            var response = await _httpClient.PostAsync("executions", content, cancellationToken);
            var responseBody = await response.Content.ReadAsStringAsync(cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogError("Failed to execute n8n workflow. Status: {Status}, Response: {Response}",
                    response.StatusCode, responseBody);
                return N8nExecutionResult.Failed($"n8n API error: {response.StatusCode}");
            }

            var result = JsonSerializer.Deserialize<N8nExecutionResponse>(responseBody, _jsonOptions);
            if (result?.Id == null)
            {
                return N8nExecutionResult.Failed("Failed to parse n8n execution response");
            }

            _logger.LogInformation("Started n8n execution with ID: {N8nExecutionId}", result.Id);
            return N8nExecutionResult.Succeeded(result.Id);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error executing n8n workflow");
            return N8nExecutionResult.Failed(ex.Message);
        }
    }

    public async Task<N8nExecutionStatus> GetExecutionStatusAsync(
        string n8nExecutionId,
        CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Getting execution status for: {N8nExecutionId}", n8nExecutionId);

        var response = await _httpClient.GetAsync($"executions/{n8nExecutionId}", cancellationToken);
        var responseBody = await response.Content.ReadAsStringAsync(cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            return new N8nExecutionStatus
            {
                N8nExecutionId = n8nExecutionId,
                IsFailed = true,
                ErrorMessage = $"Failed to get execution status: {response.StatusCode}"
            };
        }

        var result = JsonSerializer.Deserialize<N8nExecutionDetailResponse>(responseBody, _jsonOptions);

        return new N8nExecutionStatus
        {
            N8nExecutionId = n8nExecutionId,
            IsRunning = result?.Status == "running",
            IsCompleted = result?.Status == "success",
            IsFailed = result?.Status == "error",
            IsWaitingForApproval = result?.Status == "waiting",
            OutputJson = result?.Data != null ? JsonSerializer.Serialize(result.Data, _jsonOptions) : null,
            ErrorMessage = result?.Status == "error" ? "Execution failed" : null
        };
    }

    public async Task ResumeExecutionAsync(string n8nExecutionId, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Resuming n8n execution: {N8nExecutionId}", n8nExecutionId);

        var response = await _httpClient.PostAsync($"executions/{n8nExecutionId}/resume", null, cancellationToken);
        response.EnsureSuccessStatusCode();
    }

    public async Task DeleteWorkflowAsync(string n8nWorkflowId, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Deleting n8n workflow: {N8nWorkflowId}", n8nWorkflowId);

        var response = await _httpClient.DeleteAsync($"workflows/{n8nWorkflowId}", cancellationToken);
        response.EnsureSuccessStatusCode();
    }

    private class N8nWorkflowResponse
    {
        public string? Id { get; set; }
        public string? Name { get; set; }
        public bool Active { get; set; }
    }

    private class N8nExecutionResponse
    {
        public string? Id { get; set; }
        public string? Status { get; set; }
    }

    private class N8nExecutionDetailResponse
    {
        public string? Id { get; set; }
        public string? Status { get; set; }
        public object? Data { get; set; }
    }

    private class FlowBuilderResponse
    {
        public bool Success { get; set; }
        public string? WorkflowId { get; set; }
        public string? WorkflowName { get; set; }
        public string? SpecId { get; set; }
        public int SpecVersion { get; set; }
        public List<string>? Warnings { get; set; }
        public string? CompiledAt { get; set; }
        public string? Error { get; set; }
    }

    private class FlowRunnerResponse
    {
        public bool Success { get; set; }
        public bool Async { get; set; }
        public string? RunId { get; set; }
        public string? ExecutionId { get; set; }
        public string? Status { get; set; }
        public object? Result { get; set; }
        public string? Error { get; set; }
        public string? StartedAt { get; set; }
        public string? FinishedAt { get; set; }
    }
}
