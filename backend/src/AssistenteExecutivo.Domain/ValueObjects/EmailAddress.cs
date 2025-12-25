using System.Text.RegularExpressions;
using AssistenteExecutivo.Domain.Exceptions;

namespace AssistenteExecutivo.Domain.ValueObjects;

public sealed class EmailAddress : IEquatable<EmailAddress>
{
    public string Value { get; }

    private EmailAddress(string value)
    {
        var normalizedEmail = Normalize(value);
        
        if (!Validate(normalizedEmail))
            throw new DomainException("Domain:EmailInvalido", value);

        Value = normalizedEmail;
    }

    public static EmailAddress Create(string value)
    {
        return new EmailAddress(value);
    }

    private static string Normalize(string email)
    {
        return (email ?? string.Empty).Trim().ToLowerInvariant();
    }

    private static bool Validate(string email)
    {
        if (string.IsNullOrWhiteSpace(email))
            return false;

        var pattern = @"^[a-zA-Z0-9._%+-]+@[a-zA-Z0-9.-]+\.[a-zA-Z]{2,}$";
        return Regex.IsMatch(email, pattern);
    }

    public bool Equals(EmailAddress? other)
    {
        if (other is null) return false;
        return Value == other.Value;
    }

    public override bool Equals(object? obj)
    {
        return obj is EmailAddress other && Equals(other);
    }

    public override int GetHashCode()
    {
        return Value.GetHashCode();
    }

    public override string ToString()
    {
        return Value;
    }

    public static bool operator ==(EmailAddress? left, EmailAddress? right)
    {
        return Equals(left, right);
    }

    public static bool operator !=(EmailAddress? right, EmailAddress? left)
    {
        return !Equals(left, right);
    }
}

