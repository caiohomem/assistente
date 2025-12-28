using AssistenteExecutivo.Domain.Enums;
using AssistenteExecutivo.Domain.Exceptions;
using AssistenteExecutivo.Domain.Interfaces;
using AssistenteExecutivo.Domain.ValueObjects;

namespace AssistenteExecutivo.Domain.Entities;

public class Plan
{
    private readonly List<string> _features = new();

    private Plan() { } // EF Core

    public Plan(
        Guid planId,
        string name,
        decimal price,
        string currency,
        BillingInterval interval,
        IClock clock)
    {
        if (planId == Guid.Empty)
            throw new DomainException("Domain:PlanIdObrigatorio");

        if (string.IsNullOrWhiteSpace(name))
            throw new DomainException("Domain:PlanNameObrigatorio");

        if (price < 0)
            throw new DomainException("Domain:PlanPriceDeveSerPositivo");

        if (string.IsNullOrWhiteSpace(currency))
            throw new DomainException("Domain:PlanCurrencyObrigatorio");

        if (clock == null)
            throw new DomainException("Domain:ClockObrigatorio");

        PlanId = planId;
        Name = name;
        Price = price;
        Currency = currency;
        Interval = interval;
        IsActive = true;
        Highlighted = false;
        CreatedAt = clock.UtcNow;
        UpdatedAt = clock.UtcNow;
    }

    public Guid PlanId { get; private set; }
    public string Name { get; private set; } = string.Empty;
    public decimal Price { get; private set; }
    public string Currency { get; private set; } = string.Empty;
    public BillingInterval Interval { get; private set; }
    public IReadOnlyCollection<string> Features => _features.AsReadOnly();
    public PlanLimits? Limits { get; private set; }
    public bool IsActive { get; private set; }
    public bool Highlighted { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime UpdatedAt { get; private set; }

    public void AddFeature(string feature)
    {
        if (string.IsNullOrWhiteSpace(feature))
            throw new DomainException("Domain:FeatureObrigatorio");

        if (_features.Contains(feature))
            return; // Já existe, idempotente

        _features.Add(feature);
    }

    public void RemoveFeature(string feature)
    {
        if (string.IsNullOrWhiteSpace(feature))
            return;

        _features.Remove(feature);
    }

    public void SetLimits(PlanLimits limits)
    {
        if (limits == null)
            throw new DomainException("Domain:PlanLimitsObrigatorio");

        Limits = limits;
    }

    public void Activate()
    {
        IsActive = true;
    }

    public void Deactivate()
    {
        IsActive = false;
    }

    public void SetHighlighted(bool highlighted)
    {
        Highlighted = highlighted;
    }

    public void UpdatePrice(decimal newPrice, IClock clock)
    {
        if (newPrice < 0)
            throw new DomainException("Domain:PlanPriceDeveSerPositivo");

        if (clock == null)
            throw new DomainException("Domain:ClockObrigatorio");

        Price = newPrice;
        UpdatedAt = clock.UtcNow;
    }
}










