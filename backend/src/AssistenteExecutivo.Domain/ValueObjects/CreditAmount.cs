using AssistenteExecutivo.Domain.Exceptions;

namespace AssistenteExecutivo.Domain.ValueObjects;

public sealed class CreditAmount : ValueObject
{
    public decimal Value { get; private set; }

    private CreditAmount() { } // EF Core

    private CreditAmount(decimal value)
    {
        if (value < 0)
            throw new DomainException("Domain:CreditAmountNaoPodeSerNegativo");

        Value = value;
    }

    public static CreditAmount Create(decimal value)
    {
        return new CreditAmount(value);
    }

    public static CreditAmount Zero => new(0);

    public static CreditAmount operator +(CreditAmount left, CreditAmount right)
    {
        return new CreditAmount(left.Value + right.Value);
    }

    public static CreditAmount operator -(CreditAmount left, CreditAmount right)
    {
        var result = left.Value - right.Value;
        if (result < 0)
            throw new DomainException("Domain:CreditAmountResultadoNegativo");
        return new CreditAmount(result);
    }

    public static bool operator >(CreditAmount left, CreditAmount right)
    {
        return left.Value > right.Value;
    }

    public static bool operator <(CreditAmount left, CreditAmount right)
    {
        return left.Value < right.Value;
    }

    public static bool operator >=(CreditAmount left, CreditAmount right)
    {
        return left.Value >= right.Value;
    }

    public static bool operator <=(CreditAmount left, CreditAmount right)
    {
        return left.Value <= right.Value;
    }

    protected override IEnumerable<object> GetEqualityComponents()
    {
        yield return Value;
    }
}

