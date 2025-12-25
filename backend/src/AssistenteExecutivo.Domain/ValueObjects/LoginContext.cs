namespace AssistenteExecutivo.Domain.ValueObjects;

public sealed class LoginContext : ValueObject
{
    public string? IpAddress { get; }
    public string? UserAgent { get; }
    public Enums.AuthMethod AuthMethod { get; }
    public string? CorrelationId { get; }

    public LoginContext(
        string? ipAddress,
        string? userAgent,
        Enums.AuthMethod authMethod,
        string? correlationId = null)
    {
        IpAddress = ipAddress;
        UserAgent = userAgent;
        AuthMethod = authMethod;
        CorrelationId = correlationId;
    }

    protected override IEnumerable<object> GetEqualityComponents()
    {
        yield return IpAddress ?? string.Empty;
        yield return UserAgent ?? string.Empty;
        yield return AuthMethod;
        yield return CorrelationId ?? string.Empty;
    }
}

