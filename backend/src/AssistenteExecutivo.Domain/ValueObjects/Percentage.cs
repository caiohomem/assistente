using AssistenteExecutivo.Domain.Exceptions;

namespace AssistenteExecutivo.Domain.ValueObjects;

public sealed class Percentage : ValueObject
{
    public decimal Value { get; private set; }

    private Percentage() { } // EF Core

    private Percentage(decimal value)
    {
        if (value < 0 || value > 100)
            throw new DomainException("Domain:PercentageDeveEstarEntreZeroECem");
        Value = value;
    }

    public static Percentage Create(decimal value)
    {
        return new Percentage(value);
    }

    public static Percentage Zero => new(0);
    public static Percentage Full => new(100);

    public Money CalculateFrom(Money total)
    {
        return total.MultiplyBy(Value / 100m);
    }

    public static Percentage operator +(Percentage left, Percentage right)
    {
        var result = left.Value + right.Value;
        if (result > 100)
            throw new DomainException("Domain:PercentageSomaMaiorQueCem");
        return new Percentage(result);
    }

    public static Percentage operator -(Percentage left, Percentage right)
    {
        var result = left.Value - right.Value;
        if (result < 0)
            throw new DomainException("Domain:PercentageResultadoNegativo");
        return new Percentage(result);
    }

    public static bool operator >(Percentage left, Percentage right)
    {
        return left.Value > right.Value;
    }

    public static bool operator <(Percentage left, Percentage right)
    {
        return left.Value < right.Value;
    }

    public static bool operator >=(Percentage left, Percentage right)
    {
        return left.Value >= right.Value;
    }

    public static bool operator <=(Percentage left, Percentage right)
    {
        return left.Value <= right.Value;
    }

    protected override IEnumerable<object> GetEqualityComponents()
    {
        yield return Value;
    }
}
