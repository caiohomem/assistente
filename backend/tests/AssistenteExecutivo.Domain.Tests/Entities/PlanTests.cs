using AssistenteExecutivo.Domain.Entities;
using AssistenteExecutivo.Domain.Enums;
using AssistenteExecutivo.Domain.Exceptions;
using AssistenteExecutivo.Domain.Interfaces;
using AssistenteExecutivo.Domain.ValueObjects;
using FluentAssertions;

namespace AssistenteExecutivo.Domain.Tests.Entities;

public class PlanTests
{
    private readonly IClock _clock;

    public PlanTests()
    {
        _clock = new TestClock();
    }

    [Fact]
    public void Constructor_ValidData_ShouldCreate()
    {
        // Arrange
        var planId = Guid.NewGuid();
        var name = "Basic Plan";
        var price = 29.99m;
        var currency = "USD";
        var interval = BillingInterval.Monthly;

        // Act
        var plan = new Plan(planId, name, price, currency, interval, _clock);

        // Assert
        plan.PlanId.Should().Be(planId);
        plan.Name.Should().Be(name);
        plan.Price.Should().Be(price);
        plan.Currency.Should().Be(currency);
        plan.Interval.Should().Be(interval);
        plan.IsActive.Should().BeTrue();
        plan.Highlighted.Should().BeFalse();
        plan.Features.Should().BeEmpty();
        plan.Limits.Should().BeNull();
    }

    [Fact]
    public void Constructor_EmptyPlanId_ShouldThrow()
    {
        // Act & Assert
        var act = () => new Plan(Guid.Empty, "Basic", 29.99m, "USD", BillingInterval.Monthly, _clock);
        act.Should().Throw<DomainException>()
            .WithMessage("*PlanIdObrigatorio*");
    }

    [Fact]
    public void Constructor_EmptyName_ShouldThrow()
    {
        // Act & Assert
        var act = () => new Plan(Guid.NewGuid(), "", 29.99m, "USD", BillingInterval.Monthly, _clock);
        act.Should().Throw<DomainException>()
            .WithMessage("*PlanNameObrigatorio*");
    }

    [Fact]
    public void Constructor_NegativePrice_ShouldThrow()
    {
        // Act & Assert
        var act = () => new Plan(Guid.NewGuid(), "Basic", -10m, "USD", BillingInterval.Monthly, _clock);
        act.Should().Throw<DomainException>()
            .WithMessage("*PlanPriceDeveSerPositivo*");
    }

    [Fact]
    public void Constructor_EmptyCurrency_ShouldThrow()
    {
        // Act & Assert
        var act = () => new Plan(Guid.NewGuid(), "Basic", 29.99m, "", BillingInterval.Monthly, _clock);
        act.Should().Throw<DomainException>()
            .WithMessage("*PlanCurrencyObrigatorio*");
    }

    [Fact]
    public void Constructor_NullClock_ShouldThrow()
    {
        // Act & Assert
        var act = () => new Plan(Guid.NewGuid(), "Basic", 29.99m, "USD", BillingInterval.Monthly, null!);
        act.Should().Throw<DomainException>()
            .WithMessage("*ClockObrigatorio*");
    }

    [Fact]
    public void AddFeature_ValidFeature_ShouldAdd()
    {
        // Arrange
        var plan = CreatePlan();
        var feature = "Feature 1";

        // Act
        plan.AddFeature(feature);

        // Assert
        plan.Features.Should().ContainSingle().Which.Should().Be(feature);
    }

    [Fact]
    public void AddFeature_EmptyFeature_ShouldThrow()
    {
        // Arrange
        var plan = CreatePlan();

        // Act & Assert
        var act = () => plan.AddFeature("");
        act.Should().Throw<DomainException>()
            .WithMessage("*FeatureObrigatorio*");
    }

    [Fact]
    public void AddFeature_DuplicateFeature_ShouldNotAdd()
    {
        // Arrange
        var plan = CreatePlan();
        var feature = "Feature 1";
        plan.AddFeature(feature);

        // Act
        plan.AddFeature(feature);

        // Assert
        plan.Features.Should().ContainSingle().Which.Should().Be(feature);
    }

    [Fact]
    public void RemoveFeature_ExistingFeature_ShouldRemove()
    {
        // Arrange
        var plan = CreatePlan();
        var feature = "Feature 1";
        plan.AddFeature(feature);

        // Act
        plan.RemoveFeature(feature);

        // Assert
        plan.Features.Should().BeEmpty();
    }

    [Fact]
    public void RemoveFeature_NonExistentFeature_ShouldNotThrow()
    {
        // Arrange
        var plan = CreatePlan();

        // Act
        var act = () => plan.RemoveFeature("Non-existent");

        // Assert
        act.Should().NotThrow();
        plan.Features.Should().BeEmpty();
    }

    [Fact]
    public void RemoveFeature_EmptyFeature_ShouldNotThrow()
    {
        // Arrange
        var plan = CreatePlan();

        // Act
        var act = () => plan.RemoveFeature("");

        // Assert
        act.Should().NotThrow();
    }

    [Fact]
    public void SetLimits_ValidLimits_ShouldSet()
    {
        // Arrange
        var plan = CreatePlan();
        var limits = new PlanLimits(contacts: 100, notes: 500, creditsPerMonth: 1000, storageGB: 10m);

        // Act
        plan.SetLimits(limits);

        // Assert
        plan.Limits.Should().Be(limits);
    }

    [Fact]
    public void SetLimits_NullLimits_ShouldThrow()
    {
        // Arrange
        var plan = CreatePlan();

        // Act & Assert
        var act = () => plan.SetLimits(null!);
        act.Should().Throw<DomainException>()
            .WithMessage("*PlanLimitsObrigatorio*");
    }

    [Fact]
    public void Activate_ShouldSetIsActiveToTrue()
    {
        // Arrange
        var plan = CreatePlan();
        plan.Deactivate();

        // Act
        plan.Activate();

        // Assert
        plan.IsActive.Should().BeTrue();
    }

    [Fact]
    public void Deactivate_ShouldSetIsActiveToFalse()
    {
        // Arrange
        var plan = CreatePlan();

        // Act
        plan.Deactivate();

        // Assert
        plan.IsActive.Should().BeFalse();
    }

    [Fact]
    public void SetHighlighted_True_ShouldSetHighlighted()
    {
        // Arrange
        var plan = CreatePlan();

        // Act
        plan.SetHighlighted(true);

        // Assert
        plan.Highlighted.Should().BeTrue();
    }

    [Fact]
    public void SetHighlighted_False_ShouldUnsetHighlighted()
    {
        // Arrange
        var plan = CreatePlan();
        plan.SetHighlighted(true);

        // Act
        plan.SetHighlighted(false);

        // Assert
        plan.Highlighted.Should().BeFalse();
    }

    [Fact]
    public void UpdatePrice_ValidPrice_ShouldUpdate()
    {
        // Arrange
        var plan = CreatePlan();
        var newPrice = 39.99m;

        // Act
        plan.UpdatePrice(newPrice, _clock);

        // Assert
        plan.Price.Should().Be(newPrice);
        plan.UpdatedAt.Should().BeCloseTo(_clock.UtcNow, TimeSpan.FromSeconds(1));
    }

    [Fact]
    public void UpdatePrice_NegativePrice_ShouldThrow()
    {
        // Arrange
        var plan = CreatePlan();

        // Act & Assert
        var act = () => plan.UpdatePrice(-10m, _clock);
        act.Should().Throw<DomainException>()
            .WithMessage("*PlanPriceDeveSerPositivo*");
    }

    [Fact]
    public void UpdatePrice_NullClock_ShouldThrow()
    {
        // Arrange
        var plan = CreatePlan();

        // Act & Assert
        var act = () => plan.UpdatePrice(39.99m, null!);
        act.Should().Throw<DomainException>()
            .WithMessage("*ClockObrigatorio*");
    }

    private Plan CreatePlan()
    {
        return new Plan(
            Guid.NewGuid(),
            "Basic Plan",
            29.99m,
            "USD",
            BillingInterval.Monthly,
            _clock);
    }

    private class TestClock : IClock
    {
        public DateTime UtcNow => DateTime.UtcNow;
    }
}



