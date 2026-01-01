using AssistenteExecutivo.Domain.Exceptions;
using AssistenteExecutivo.Domain.Interfaces;

namespace AssistenteExecutivo.Domain.Entities;

public class CreditPackage
{
    private CreditPackage() { } // EF Core

    public CreditPackage(
        Guid packageId,
        string name,
        decimal amount,
        decimal price,
        string currency,
        IClock clock,
        string? description = null)
    {
        if (packageId == Guid.Empty)
            throw new DomainException("Domain:PackageIdObrigatorio");

        if (string.IsNullOrWhiteSpace(name))
            throw new DomainException("Domain:PackageNameObrigatorio");

        if (amount < -1) // -1 para ilimitado
            throw new DomainException("Domain:PackageAmountInvalido");

        if (price < 0)
            throw new DomainException("Domain:PackagePriceDeveSerPositivo");

        if (string.IsNullOrWhiteSpace(currency))
            throw new DomainException("Domain:PackageCurrencyObrigatorio");

        if (clock == null)
            throw new DomainException("Domain:ClockObrigatorio");

        PackageId = packageId;
        Name = name;
        Amount = amount;
        Price = price;
        Currency = currency;
        Description = description;
        IsActive = true;
        CreatedAt = clock.UtcNow;
        UpdatedAt = clock.UtcNow;
    }

    public Guid PackageId { get; private set; }
    public string Name { get; private set; } = string.Empty;
    public decimal Amount { get; private set; } // -1 para ilimitado
    public decimal Price { get; private set; }
    public string Currency { get; private set; } = string.Empty;
    public string? Description { get; private set; }
    public bool IsActive { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime UpdatedAt { get; private set; }

    public void Activate()
    {
        IsActive = true;
    }

    public void Deactivate()
    {
        IsActive = false;
    }

    public void UpdatePrice(decimal newPrice, IClock clock)
    {
        if (newPrice < 0)
            throw new DomainException("Domain:PackagePriceDeveSerPositivo");

        if (clock == null)
            throw new DomainException("Domain:ClockObrigatorio");

        Price = newPrice;
        UpdatedAt = clock.UtcNow;
    }

    public void UpdateAmount(decimal newAmount)
    {
        if (newAmount < -1)
            throw new DomainException("Domain:PackageAmountInvalido");

        Amount = newAmount;
    }

    public void UpdateDescription(string? description)
    {
        Description = description;
    }
}











