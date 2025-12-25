using System.Net;
using System.Net.Mail;
using AssistenteExecutivo.Application.Interfaces;
using AssistenteExecutivo.Domain.Notifications;
using AssistenteExecutivo.Infrastructure.Repositories;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using EmailTemplateType = AssistenteExecutivo.Domain.Notifications.EmailTemplateType;

namespace AssistenteExecutivo.Infrastructure.Services;

public class EmailService : IEmailService
{
    private readonly IEmailTemplateRepository _templateRepository;
    private readonly IConfiguration _configuration;
    private readonly ILogger<EmailService> _logger;

    public EmailService(
        IEmailTemplateRepository templateRepository,
        IConfiguration configuration,
        ILogger<EmailService> logger)
    {
        _templateRepository = templateRepository;
        _configuration = configuration;
        _logger = logger;
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

            // Enviar email via SMTP
            await SendEmailSmtpAsync(recipientEmail, recipientName, subject, htmlBody, cancellationToken);
            
            _logger.LogInformation("Email enviado com sucesso - Template: {TemplateType}, Assunto: {Subject}, Destinatário: {RecipientEmail}", 
                templateType, subject, recipientEmail);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao enviar email com template {TemplateType} para {RecipientEmail}", templateType, recipientEmail);
            throw;
        }
    }

    private async Task SendEmailSmtpAsync(
        string recipientEmail,
        string recipientName,
        string subject,
        string htmlBody,
        CancellationToken cancellationToken)
    {
        try
        {
            var smtpHost = _configuration["Email:Smtp:Host"];
            var smtpPortStr = _configuration["Email:Smtp:Port"];
            var smtpUser = _configuration["Email:Smtp:User"];
            var smtpPassword = _configuration["Email:Smtp:Password"];
            var smtpFrom = _configuration["Email:Smtp:From"] ?? "noreply@assistenteexecutivo.com";
            var smtpFromName = _configuration["Email:Smtp:FromName"] ?? "Assistente Executivo";
            var smtpEnableSsl = _configuration.GetValue<bool>("Email:Smtp:EnableSsl", true);

            if (string.IsNullOrWhiteSpace(smtpHost) || string.IsNullOrWhiteSpace(smtpPortStr))
            {
                _logger.LogWarning("Configuração SMTP não encontrada. Email não será enviado. Configure Email:Smtp:Host e Email:Smtp:Port em appsettings.json");
                return;
            }

            if (!int.TryParse(smtpPortStr, out var smtpPort))
            {
                _logger.LogWarning("Porta SMTP inválida: {Port}", smtpPortStr);
                return;
            }

            using var client = new SmtpClient(smtpHost, smtpPort);
            
            if (!string.IsNullOrWhiteSpace(smtpUser) && !string.IsNullOrWhiteSpace(smtpPassword))
            {
                client.Credentials = new NetworkCredential(smtpUser, smtpPassword);
                client.EnableSsl = smtpEnableSsl;
            }
            else
            {
                // Para desenvolvimento local (smtp4dev não precisa credenciais)
                client.EnableSsl = false;
            }

            var mailMessage = new MailMessage
            {
                From = new MailAddress(smtpFrom, smtpFromName),
                Subject = subject,
                Body = htmlBody,
                IsBodyHtml = true
            };

            mailMessage.To.Add(new MailAddress(recipientEmail, recipientName ?? recipientEmail));

            await client.SendMailAsync(mailMessage, cancellationToken);
            
            _logger.LogInformation("Email SMTP enviado com sucesso para {RecipientEmail}", recipientEmail);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao enviar email SMTP para {RecipientEmail}", recipientEmail);
            throw;
        }
    }
}

