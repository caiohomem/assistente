using AssistenteExecutivo.Application.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using EmailTemplateType = AssistenteExecutivo.Domain.Notifications.EmailTemplateType;

namespace AssistenteExecutivo.Infrastructure.Services;

public class EmailService : IEmailService
{
    private readonly IEmailTemplateRepository _templateRepository;
    private readonly IConfiguration _configuration;
    private readonly ILogger<EmailService> _logger;
    private readonly IHttpClientFactory _httpClientFactory;

    public EmailService(
        IEmailTemplateRepository templateRepository,
        IConfiguration configuration,
        ILogger<EmailService> logger,
        IHttpClientFactory httpClientFactory)
    {
        _templateRepository = templateRepository;
        _configuration = configuration;
        _logger = logger;
        _httpClientFactory = httpClientFactory;
    }

    public async Task SendEmailWithTemplateAsync(
        EmailTemplateType templateType,
        string recipientEmail,
        string recipientName,
        Dictionary<string, object> templateValues,
        CancellationToken cancellationToken = default)
    {
        try
        {
            // Buscar template ativo
            var template = await _templateRepository.GetByTypeAsync(templateType, cancellationToken);

            if (template == null || !template.IsActive)
            {
                _logger.LogWarning("Template de email {TemplateType} não encontrado ou inativo. Email não será enviado.", templateType);
                return;
            }

            // Aplicar valores ao template
            var subject = template.ApplySubject(templateValues);
            var htmlBody = template.ApplyTemplate(templateValues);

            // Enviar email via Mailjet API
            await SendEmailViaMailjetAsync(recipientEmail, recipientName, subject, htmlBody, cancellationToken);

            _logger.LogInformation("Email enviado com sucesso - Template: {TemplateType}, Assunto: {Subject}, Destinatário: {RecipientEmail}",
                templateType, subject, recipientEmail);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao enviar email com template {TemplateType} para {RecipientEmail}", templateType, recipientEmail);
            throw;
        }
    }

    public async Task SendEmailAsync(
        string recipientEmail,
        string recipientName,
        string subject,
        string htmlBody,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(recipientEmail))
        {
            _logger.LogWarning("Email não enviado: destinatário vazio.");
            return;
        }

        if (string.IsNullOrWhiteSpace(subject) || string.IsNullOrWhiteSpace(htmlBody))
        {
            _logger.LogWarning("Email não enviado: assunto ou corpo vazios.");
            return;
        }

        await SendEmailViaMailjetAsync(recipientEmail, recipientName, subject, htmlBody, cancellationToken);
    }

    private async Task SendEmailViaMailjetAsync(
        string recipientEmail,
        string recipientName,
        string subject,
        string htmlBody,
        CancellationToken cancellationToken)
    {
        try
        {
            var apiKey = _configuration["Email:Mailjet:ApiKey"];
            var secretKey = _configuration["Email:Mailjet:SecretKey"];
            var fromEmail = _configuration["Email:Mailjet:From"] ?? _configuration["Email:From"] ?? "noreply@assistenteexecutivo.com";
            var fromName = _configuration["Email:Mailjet:FromName"] ?? _configuration["Email:FromName"] ?? "Assistente Executivo";

            if (string.IsNullOrWhiteSpace(apiKey) || string.IsNullOrWhiteSpace(secretKey))
            {
                _logger.LogWarning("Configuração Mailjet não encontrada. Email não será enviado. Configure Email:Mailjet:ApiKey e Email:Mailjet:SecretKey em appsettings.json");
                return;
            }

            // Criar HttpClient para esta requisição
            using var httpClient = _httpClientFactory.CreateClient();
            httpClient.Timeout = TimeSpan.FromSeconds(30);

            // Preparar autenticação Basic Auth
            var authValue = Convert.ToBase64String(Encoding.UTF8.GetBytes($"{apiKey}:{secretKey}"));
            httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", authValue);
            httpClient.DefaultRequestHeaders.Accept.Clear();
            httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

            // Preparar payload do Mailjet
            var payload = new
            {
                Messages = new[]
                {
                    new
                    {
                        From = new
                        {
                            Email = fromEmail,
                            Name = fromName
                        },
                        To = new[]
                        {
                            new
                            {
                                Email = recipientEmail,
                                Name = recipientName ?? recipientEmail
                            }
                        },
                        Subject = subject,
                        HTMLPart = htmlBody
                    }
                }
            };

            var jsonPayload = JsonSerializer.Serialize(payload);
            var content = new StringContent(jsonPayload, Encoding.UTF8, "application/json");

            // Enviar requisição para API do Mailjet
            var response = await httpClient.PostAsync("https://api.mailjet.com/v3.1/send", content, cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await response.Content.ReadAsStringAsync(cancellationToken);
                _logger.LogError("Erro ao enviar email via Mailjet. Status: {Status}, Response: {Response}",
                    response.StatusCode, errorContent);
                throw new HttpRequestException($"Erro ao enviar email via Mailjet: {response.StatusCode} - {errorContent}");
            }

            var responseContent = await response.Content.ReadAsStringAsync(cancellationToken);
            _logger.LogInformation("Email Mailjet enviado com sucesso para {RecipientEmail}. Response: {Response}",
                recipientEmail, responseContent);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao enviar email via Mailjet para {RecipientEmail}", recipientEmail);
            throw;
        }
    }
}
