using AssistenteExecutivo.Domain.Entities;
using AssistenteExecutivo.Domain.Exceptions;
using AssistenteExecutivo.Domain.Interfaces;
using FluentAssertions;

namespace AssistenteExecutivo.Domain.Tests.Entities;

public class CreditPackageTests
{
    private readonly IClock _clock;

    public CreditPackageTests()
    {
        _clock = new TestClock();
    }

    [Fact]
    public void Constructor_ValidData_ShouldCreate()
    {
        // Arrange
        var packageId = Guid.NewGuid();
        var name = "Basic Package";
        var amount = 100m;
        var price = 9.99m;
        var currency = "USD";
        var description = "Basic credit package";

        // Act
        var package = new CreditPackage(packageId, name, amount, price, currency, _clock, description);

        // Assert
        package.PackageId.Should().Be(packageId);
        package.Name.Should().Be(name);
        package.Amount.Should().Be(amount);
        package.Price.Should().Be(price);
        package.Currency.Should().Be(currency);
        package.Description.Should().Be(description);
        package.IsActive.Should().BeTrue();
    }

    [Fact]
    public void Constructor_WithoutDescription_ShouldCreate()
    {
        // Arrange
        var packageId = Guid.NewGuid();
        var name = "Basic Package";
        var amount = 100m;
        var price = 9.99m;
        var currency = "USD";

        // Act
        var package = new CreditPackage(packageId, name, amount, price, currency, _clock);

        // Assert
        package.Description.Should().BeNull();
    }

    [Fact]
    public void Constructor_UnlimitedAmount_ShouldCreate()
    {
        // Arrange
        var packageId = Guid.NewGuid();
        var name = "Unlimited Package";
        var amount = -1m; // -1 para ilimitado
        var price = 99.99m;
        var currency = "USD";

        // Act
        var package = new CreditPackage(packageId, name, amount, price, currency, _clock);

        // Assert
        package.Amount.Should().Be(-1m);
    }

    [Fact]
    public void Constructor_EmptyPackageId_ShouldThrow()
    {
        // Act & Assert
        var act = () => new CreditPackage(Guid.Empty, "Package", 100m, 9.99m, "USD", _clock);
        act.Should().Throw<DomainException>()
            .WithMessage("*PackageIdObrigatorio*");
    }

    [Fact]
    public void Constructor_EmptyName_ShouldThrow()
    {
        // Act & Assert
        var act = () => new CreditPackage(Guid.NewGuid(), "", 100m, 9.99m, "USD", _clock);
        act.Should().Throw<DomainException>()
            .WithMessage("*PackageNameObrigatorio*");
    }

    [Fact]
    public void Constructor_InvalidAmount_ShouldThrow()
    {
        // Act & Assert
        var act = () => new CreditPackage(Guid.NewGuid(), "Package", -2m, 9.99m, "USD", _clock);
        act.Should().Throw<DomainException>()
            .WithMessage("*PackageAmountInvalido*");
    }

    [Fact]
    public void Constructor_NegativePrice_ShouldThrow()
    {
        // Act & Assert
        var act = () => new CreditPackage(Guid.NewGuid(), "Package", 100m, -10m, "USD", _clock);
        act.Should().Throw<DomainException>()
            .WithMessage("*PackagePriceDeveSerPositivo*");
    }

    [Fact]
    public void Constructor_EmptyCurrency_ShouldThrow()
    {
        // Act & Assert
        var act = () => new CreditPackage(Guid.NewGuid(), "Package", 100m, 9.99m, "", _clock);
        act.Should().Throw<DomainException>()
            .WithMessage("*PackageCurrencyObrigatorio*");
    }

    [Fact]
    public void Constructor_NullClock_ShouldThrow()
    {
        // Act & Assert
        var act = () => new CreditPackage(Guid.NewGuid(), "Package", 100m, 9.99m, "USD", null!);
        act.Should().Throw<DomainException>()
            .WithMessage("*ClockObrigatorio*");
    }

    [Fact]
    public void Activate_ShouldSetIsActiveToTrue()
    {
        // Arrange
        var package = CreatePackage();
        package.Deactivate();

        // Act
        package.Activate();

        // Assert
        package.IsActive.Should().BeTrue();
    }

    [Fact]
    public void Deactivate_ShouldSetIsActiveToFalse()
    {
        // Arrange
        var package = CreatePackage();

        // Act
        package.Deactivate();

        // Assert
        package.IsActive.Should().BeFalse();
    }

    [Fact]
    public void UpdatePrice_ValidPrice_ShouldUpdate()
    {
        // Arrange
        var package = CreatePackage();
        var newPrice = 19.99m;

        // Act
        package.UpdatePrice(newPrice, _clock);

        // Assert
        package.Price.Should().Be(newPrice);
        package.UpdatedAt.Should().BeCloseTo(_clock.UtcNow, TimeSpan.FromSeconds(1));
    }

    [Fact]
    public void UpdatePrice_NegativePrice_ShouldThrow()
    {
        // Arrange
        var package = CreatePackage();

        // Act & Assert
        var act = () => package.UpdatePrice(-10m, _clock);
        act.Should().Throw<DomainException>()
            .WithMessage("*PackagePriceDeveSerPositivo*");
    }

    [Fact]
    public void UpdatePrice_NullClock_ShouldThrow()
    {
        // Arrange
        var package = CreatePackage();

        // Act & Assert
        var act = () => package.UpdatePrice(19.99m, null!);
        act.Should().Throw<DomainException>()
            .WithMessage("*ClockObrigatorio*");
    }

    [Fact]
    public void UpdateAmount_ValidAmount_ShouldUpdate()
    {
        // Arrange
        var package = CreatePackage();
        var newAmount = 200m;

        // Act
        package.UpdateAmount(newAmount);

        // Assert
        package.Amount.Should().Be(newAmount);
    }

    [Fact]
    public void UpdateAmount_Unlimited_ShouldUpdate()
    {
        // Arrange
        var package = CreatePackage();

        // Act
        package.UpdateAmount(-1m);

        // Assert
        package.Amount.Should().Be(-1m);
    }

    [Fact]
    public void UpdateAmount_InvalidAmount_ShouldThrow()
    {
        // Arrange
        var package = CreatePackage();

        // Act & Assert
        var act = () => package.UpdateAmount(-2m);
        act.Should().Throw<DomainException>()
            .WithMessage("*PackageAmountInvalido*");
    }

    [Fact]
    public void UpdateDescription_ValidDescription_ShouldUpdate()
    {
        // Arrange
        var package = CreatePackage();
        var newDescription = "New description";

        // Act
        package.UpdateDescription(newDescription);

        // Assert
        package.Description.Should().Be(newDescription);
    }

    [Fact]
    public void UpdateDescription_Null_ShouldSetToNull()
    {
        // Arrange
        var package = CreatePackage();
        package.UpdateDescription("Some description");

        // Act
        package.UpdateDescription(null);

        // Assert
        package.Description.Should().BeNull();
    }

    private CreditPackage CreatePackage()
    {
        return new CreditPackage(
            Guid.NewGuid(),
            "Basic Package",
            100m,
            9.99m,
            "USD",
            _clock,
            "Test package");
    }

    private class TestClock : IClock
    {
        public DateTime UtcNow => DateTime.UtcNow;
    }
}



