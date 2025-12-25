using AssistenteExecutivo.Domain.Exceptions;

namespace AssistenteExecutivo.Domain.ValueObjects;

public sealed class Tag : IEquatable<Tag>
{
    public string Value { get; }

    private Tag(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            throw new DomainException("Domain:TagObrigatoria");

        if (value.Length > 50)
            throw new DomainException("Domain:TagMaximoCaracteres");

        Value = value.Trim().ToLowerInvariant();
    }

    public static Tag Create(string value)
    {
        return new Tag(value);
    }

    public bool Equals(Tag? other)
    {
        if (other is null) return false;
        return Value == other.Value;
    }

    public override bool Equals(object? obj)
    {
        return obj is Tag other && Equals(other);
    }

    public override int GetHashCode()
    {
        return Value.GetHashCode();
    }

    public override string ToString()
    {
        return Value;
    }

    public static bool operator ==(Tag? left, Tag? right)
    {
        return Equals(left, right);
    }

    public static bool operator !=(Tag? left, Tag? right)
    {
        return !Equals(left, right);
    }
}

