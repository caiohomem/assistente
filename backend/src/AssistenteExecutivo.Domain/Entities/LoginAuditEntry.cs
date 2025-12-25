using AssistenteExecutivo.Domain.Enums;
using AssistenteExecutivo.Domain.ValueObjects;

namespace AssistenteExecutivo.Domain.Entities;

public class LoginAuditEntry
{
    private LoginAuditEntry() { } // EF Core

    public LoginAuditEntry(
        Guid userId,
        LoginContext loginContext,
        DateTime occurredAt)
    {
        UserId = userId;
        IpAddress = loginContext.IpAddress;
        UserAgent = loginContext.UserAgent;
        AuthMethod = loginContext.AuthMethod;
        CorrelationId = loginContext.CorrelationId;
        OccurredAt = occurredAt;
    }

    public int Id { get; private set; }
    public Guid UserId { get; private set; }
    public string? IpAddress { get; private set; }
    public string? UserAgent { get; private set; }
    public AuthMethod AuthMethod { get; private set; }
    public string? CorrelationId { get; private set; }
    public DateTime OccurredAt { get; private set; }
}

