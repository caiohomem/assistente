using AssistenteExecutivo.Domain.Exceptions;

namespace AssistenteExecutivo.Domain.ValueObjects;

public sealed class IdempotencyKey : IEquatable<IdempotencyKey>
{
    public string Value { get; }

    private IdempotencyKey(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            throw new DomainException("Domain:IdempotencyKeyObrigatorio");

        if (value.Length < 8)
            throw new DomainException("Domain:IdempotencyKeyMinimoCaracteres");

        Value = value;
    }

    public static IdempotencyKey Create(string value)
    {
        return new IdempotencyKey(value);
    }

    public static IdempotencyKey Generate() => Create(Guid.NewGuid().ToString("N"));

    public bool Equals(IdempotencyKey? other)
    {
        if (other is null) return false;
        return Value == other.Value;
    }

    public override bool Equals(object? obj)
    {
        return obj is IdempotencyKey other && Equals(other);
    }

    public override int GetHashCode()
    {
        return Value.GetHashCode();
    }

    public override string ToString()
    {
        return Value;
    }

    public static bool operator ==(IdempotencyKey? left, IdempotencyKey? right)
    {
        return Equals(left, right);
    }

    public static bool operator !=(IdempotencyKey? left, IdempotencyKey? right)
    {
        return !Equals(left, right);
    }
}

