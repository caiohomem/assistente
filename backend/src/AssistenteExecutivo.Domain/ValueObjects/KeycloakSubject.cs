using AssistenteExecutivo.Domain.Exceptions;

namespace AssistenteExecutivo.Domain.ValueObjects;

public sealed class KeycloakSubject : IEquatable<KeycloakSubject>
{
    public string Value { get; }

    private KeycloakSubject(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            throw new DomainException("Domain:KeycloakSubjectObrigatorio");

        Value = value;
    }

    public static KeycloakSubject Create(string value)
    {
        return new KeycloakSubject(value);
    }

    public bool Equals(KeycloakSubject? other)
    {
        if (other is null) return false;
        return Value == other.Value;
    }

    public override bool Equals(object? obj)
    {
        return obj is KeycloakSubject other && Equals(other);
    }

    public override int GetHashCode()
    {
        return Value.GetHashCode();
    }

    public override string ToString()
    {
        return Value;
    }

    public static bool operator ==(KeycloakSubject? left, KeycloakSubject? right)
    {
        return Equals(left, right);
    }

    public static bool operator !=(KeycloakSubject? left, KeycloakSubject? right)
    {
        return !Equals(left, right);
    }
}

