using AssistenteExecutivo.Domain.Notifications;

namespace AssistenteExecutivo.Application.Interfaces;

public interface IEmailService
{
    Task SendEmailWithTemplateAsync(
        EmailTemplateType templateType,
        string recipientEmail,
        string recipientName,
        Dictionary<string, object> templateValues,
        CancellationToken cancellationToken = default);
}

