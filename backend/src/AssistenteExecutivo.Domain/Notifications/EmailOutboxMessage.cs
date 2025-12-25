using AssistenteExecutivo.Domain.Exceptions;

namespace AssistenteExecutivo.Domain.Notifications;

/// <summary>
/// Mensagem de email para envio assíncrono (outbox pattern).
/// Opcional para MVP, mas útil para garantir entrega e retry.
/// </summary>
public class EmailOutboxMessage
{
    private EmailOutboxMessage() { } // EF Core

    public EmailOutboxMessage(
        string recipientEmail,
        string recipientName,
        string subject,
        string htmlBody,
        DateTime createdAt)
    {
        if (string.IsNullOrWhiteSpace(recipientEmail))
            throw new DomainException("Email do destinatário é obrigatório");

        if (string.IsNullOrWhiteSpace(subject))
            throw new DomainException("Assunto do email é obrigatório");

        if (string.IsNullOrWhiteSpace(htmlBody))
            throw new DomainException("Corpo HTML do email é obrigatório");

        Id = Guid.NewGuid();
        RecipientEmail = recipientEmail.Trim();
        RecipientName = recipientName?.Trim();
        Subject = subject.Trim();
        HtmlBody = htmlBody;
        Status = EmailOutboxStatus.Pending;
        CreatedAt = createdAt;
        RetryCount = 0;
    }

    public Guid Id { get; private set; }
    public string RecipientEmail { get; private set; }
    public string? RecipientName { get; private set; }
    public string Subject { get; private set; }
    public string HtmlBody { get; private set; }
    public EmailOutboxStatus Status { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime? SentAt { get; private set; }
    public DateTime? FailedAt { get; private set; }
    public string? ErrorMessage { get; private set; }
    public int RetryCount { get; private set; }
    public DateTime? NextRetryAt { get; private set; }

    public void MarkAsSent()
    {
        Status = EmailOutboxStatus.Sent;
        SentAt = DateTime.UtcNow;
        ErrorMessage = null;
    }

    public void MarkAsFailed(string errorMessage)
    {
        Status = EmailOutboxStatus.Failed;
        FailedAt = DateTime.UtcNow;
        ErrorMessage = errorMessage;
    }

    public void ScheduleRetry(DateTime nextRetryAt)
    {
        Status = EmailOutboxStatus.Pending;
        RetryCount++;
        NextRetryAt = nextRetryAt;
        ErrorMessage = null;
    }

    public bool ShouldRetry(int maxRetries = 3)
    {
        return Status == EmailOutboxStatus.Failed 
            && RetryCount < maxRetries
            && (NextRetryAt == null || DateTime.UtcNow >= NextRetryAt);
    }
}

public enum EmailOutboxStatus
{
    Pending = 1,
    Sent = 2,
    Failed = 3
}

