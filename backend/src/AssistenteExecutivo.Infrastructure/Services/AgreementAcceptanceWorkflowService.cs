using AssistenteExecutivo.Application.Interfaces;
using AssistenteExecutivo.Domain.Entities;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.Text;
using System.Text.Json;

namespace AssistenteExecutivo.Infrastructure.Services;

public class AgreementAcceptanceWorkflowService : IAgreementAcceptanceWorkflowService
{
    private const int DefaultMaxDays = 7;
    private const int DefaultReminderIntervalDays = 1;

    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<AgreementAcceptanceWorkflowService> _logger;
    private readonly string _webhookBaseUrl;
    private readonly string _workflowPath;
    private readonly string? _emailTemplateName;
    private readonly string? _reminderTemplateName;

    public AgreementAcceptanceWorkflowService(
        IConfiguration configuration,
        IHttpClientFactory httpClientFactory,
        ILogger<AgreementAcceptanceWorkflowService> logger)
    {
        _httpClientFactory = httpClientFactory;
        _logger = logger;

        var baseUrl = configuration["N8n:WebhookBaseUrl"] ?? string.Empty;
        if (string.IsNullOrWhiteSpace(baseUrl))
        {
            var n8nBase = configuration["N8n:BaseUrl"] ?? string.Empty;
            if (!string.IsNullOrWhiteSpace(n8nBase))
            {
                baseUrl = $"{n8nBase.TrimEnd('/')}/webhook/";
            }
        }

        _webhookBaseUrl = baseUrl;
        _workflowPath = configuration["N8n:AgreementAcceptanceWebhookPath"] ?? "commission-acceptance";
        _emailTemplateName = configuration["N8n:AgreementAcceptanceEmailTemplateName"];
        _reminderTemplateName = configuration["N8n:AgreementAcceptanceReminderTemplateName"];
    }

    public async Task StartAsync(CommissionAgreement agreement, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(_webhookBaseUrl))
        {
            _logger.LogWarning("Webhook do n8n não configurado para fluxo de aceite.");
            return;
        }

        var payload = new
        {
            agreementId = agreement.AgreementId,
            ownerUserId = agreement.OwnerUserId,
            maxDays = DefaultMaxDays,
            reminderIntervalDays = DefaultReminderIntervalDays,
            emailTemplateName = _emailTemplateName,
            reminderTemplateName = _reminderTemplateName
        };

        var url = $"{_webhookBaseUrl.TrimEnd('/')}/{_workflowPath.TrimStart('/')}";
        var client = _httpClientFactory.CreateClient();
        var content = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json");

        var response = await client.PostAsync(url, content, cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            _logger.LogWarning("Falha ao disparar workflow de aceite. Status {Status}", response.StatusCode);
        }
    }
}
