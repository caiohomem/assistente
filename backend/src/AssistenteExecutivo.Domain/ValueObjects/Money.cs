using AssistenteExecutivo.Domain.Exceptions;

namespace AssistenteExecutivo.Domain.ValueObjects;

public sealed class Money : ValueObject
{
    public decimal Amount { get; private set; }
    public string Currency { get; private set; } = "BRL";

    private Money() { } // EF Core

    private Money(decimal amount, string currency)
    {
        if (amount < 0)
            throw new DomainException("Domain:MoneyAmountNaoPodeSerNegativo");
        if (string.IsNullOrWhiteSpace(currency))
            throw new DomainException("Domain:MoedaObrigatoria");
        if (currency.Length != 3)
            throw new DomainException("Domain:MoedaDeveSerCodigoISO");

        Amount = amount;
        Currency = currency.ToUpperInvariant();
    }

    public static Money Create(decimal amount, string currency = "BRL")
    {
        return new Money(amount, currency);
    }

    public static Money Zero(string currency = "BRL") => new(0, currency);

    public static Money operator +(Money left, Money right)
    {
        if (left.Currency != right.Currency)
            throw new DomainException("Domain:MoedasDiferentes");
        return new Money(left.Amount + right.Amount, left.Currency);
    }

    public static Money operator -(Money left, Money right)
    {
        if (left.Currency != right.Currency)
            throw new DomainException("Domain:MoedasDiferentes");
        var result = left.Amount - right.Amount;
        if (result < 0)
            throw new DomainException("Domain:MoneyResultadoNegativo");
        return new Money(result, left.Currency);
    }

    public static bool operator >(Money left, Money right)
    {
        if (left.Currency != right.Currency)
            throw new DomainException("Domain:MoedasDiferentes");
        return left.Amount > right.Amount;
    }

    public static bool operator <(Money left, Money right)
    {
        if (left.Currency != right.Currency)
            throw new DomainException("Domain:MoedasDiferentes");
        return left.Amount < right.Amount;
    }

    public static bool operator >=(Money left, Money right)
    {
        if (left.Currency != right.Currency)
            throw new DomainException("Domain:MoedasDiferentes");
        return left.Amount >= right.Amount;
    }

    public static bool operator <=(Money left, Money right)
    {
        if (left.Currency != right.Currency)
            throw new DomainException("Domain:MoedasDiferentes");
        return left.Amount <= right.Amount;
    }

    public Money MultiplyBy(decimal factor)
    {
        if (factor < 0)
            throw new DomainException("Domain:FatorNaoPodeSerNegativo");
        return new Money(Amount * factor, Currency);
    }

    protected override IEnumerable<object> GetEqualityComponents()
    {
        yield return Amount;
        yield return Currency;
    }
}
