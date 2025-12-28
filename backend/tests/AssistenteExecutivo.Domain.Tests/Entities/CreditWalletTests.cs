using AssistenteExecutivo.Domain.DomainEvents;
using AssistenteExecutivo.Domain.Entities;
using AssistenteExecutivo.Domain.Exceptions;
using AssistenteExecutivo.Domain.Interfaces;
using AssistenteExecutivo.Domain.ValueObjects;
using FluentAssertions;

namespace AssistenteExecutivo.Domain.Tests.Entities;

public class CreditWalletTests
{
    private readonly IClock _clock;

    public CreditWalletTests()
    {
        _clock = new TestClock();
    }

    [Fact]
    public void Grant_ShouldAddCredits()
    {
        // Arrange
        var wallet = CreateWallet();
        wallet.ClearDomainEvents();
        var amount = CreditAmount.Create(100m);

        // Act
        wallet.Grant(amount, "Bônus inicial", _clock);

        // Assert
        wallet.Balance.Should().Be(amount);
        wallet.DomainEvents.Should().ContainSingle()
            .Which.Should().BeOfType<CreditsGranted>();
    }

    [Fact]
    public void Reserve_WithSufficientBalance_ShouldReserve()
    {
        // Arrange
        var wallet = CreateWallet();
        wallet.Grant(CreditAmount.Create(100m), "Bônus", _clock);
        wallet.ClearDomainEvents();
        var amount = CreditAmount.Create(50m);
        var idempotencyKey = IdempotencyKey.Generate();

        // Act
        wallet.Reserve(amount, idempotencyKey, "Reserva para processamento", _clock);

        // Assert
        wallet.Balance.Should().Be(CreditAmount.Create(50m));
        wallet.DomainEvents.Should().ContainSingle()
            .Which.Should().BeOfType<CreditsReserved>();
    }

    [Fact]
    public void Reserve_InsufficientBalance_ShouldThrow()
    {
        // Arrange
        var wallet = CreateWallet();
        wallet.Grant(CreditAmount.Create(30m), "Bônus", _clock);
        var amount = CreditAmount.Create(50m);
        var idempotencyKey = IdempotencyKey.Generate();

        // Act & Assert
        var act = () => wallet.Reserve(amount, idempotencyKey, "Reserva", _clock);
        act.Should().Throw<DomainException>()
            .WithMessage("*SaldoInsuficiente*");
    }

    [Fact]
    public void Reserve_WithSameIdempotencyKey_ShouldBeIdempotent()
    {
        // Arrange
        var wallet = CreateWallet();
        wallet.Grant(CreditAmount.Create(100m), "Bônus", _clock);
        var amount = CreditAmount.Create(50m);
        var idempotencyKey = IdempotencyKey.Generate();

        // Act
        wallet.Reserve(amount, idempotencyKey, "Reserva 1", _clock);
        var balanceAfterFirst = wallet.Balance;
        wallet.Reserve(amount, idempotencyKey, "Reserva 2", _clock);

        // Assert
        wallet.Balance.Should().Be(balanceAfterFirst); // Não deve deduzir novamente
    }

    [Fact]
    public void Consume_WithSufficientBalance_ShouldConsume()
    {
        // Arrange
        var wallet = CreateWallet();
        wallet.Grant(CreditAmount.Create(100m), "Bônus", _clock);
        wallet.ClearDomainEvents();
        var amount = CreditAmount.Create(30m);
        var idempotencyKey = IdempotencyKey.Generate();

        // Act
        wallet.Consume(amount, idempotencyKey, "Processamento", _clock);

        // Assert
        wallet.Balance.Should().Be(CreditAmount.Create(70m));
        wallet.DomainEvents.Should().ContainSingle()
            .Which.Should().BeOfType<CreditsConsumed>();
    }

    [Fact]
    public void Refund_ShouldAddCredits()
    {
        // Arrange
        var wallet = CreateWallet();
        wallet.Grant(CreditAmount.Create(100m), "Bônus", _clock);
        wallet.Consume(CreditAmount.Create(50m), IdempotencyKey.Generate(), "Consumo", _clock);
        wallet.ClearDomainEvents();
        var refundAmount = CreditAmount.Create(20m);
        var idempotencyKey = IdempotencyKey.Generate();

        // Act
        wallet.Refund(refundAmount, idempotencyKey, "Reembolso", _clock);

        // Assert
        wallet.Balance.Should().Be(CreditAmount.Create(70m));
        wallet.DomainEvents.Should().ContainSingle()
            .Which.Should().BeOfType<CreditsRefunded>();
    }

    [Fact]
    public void Grant_NullAmount_ShouldThrow()
    {
        // Arrange
        var wallet = CreateWallet();

        // Act & Assert
        var act = () => wallet.Grant(null!, "Reason", _clock);
        act.Should().Throw<DomainException>()
            .WithMessage("*CreditAmountObrigatorio*");
    }

    [Fact]
    public void Grant_ZeroAmount_ShouldThrow()
    {
        // Arrange
        var wallet = CreateWallet();

        // Act & Assert
        var act = () => wallet.Grant(CreditAmount.Zero, "Reason", _clock);
        act.Should().Throw<DomainException>()
            .WithMessage("*CreditAmountDeveSerPositivo*");
    }

    [Fact]
    public void Reserve_NullAmount_ShouldThrow()
    {
        // Arrange
        var wallet = CreateWallet();
        var idempotencyKey = IdempotencyKey.Generate();

        // Act & Assert
        var act = () => wallet.Reserve(null!, idempotencyKey, "Purpose", _clock);
        act.Should().Throw<DomainException>()
            .WithMessage("*CreditAmountObrigatorio*");
    }

    [Fact]
    public void Reserve_NullIdempotencyKey_ShouldThrow()
    {
        // Arrange
        var wallet = CreateWallet();
        var amount = CreditAmount.Create(50m);

        // Act & Assert
        var act = () => wallet.Reserve(amount, null!, "Purpose", _clock);
        act.Should().Throw<DomainException>()
            .WithMessage("*IdempotencyKeyObrigatorio*");
    }

    [Fact]
    public void Consume_NullAmount_ShouldThrow()
    {
        // Arrange
        var wallet = CreateWallet();
        var idempotencyKey = IdempotencyKey.Generate();

        // Act & Assert
        var act = () => wallet.Consume(null!, idempotencyKey, "Purpose", _clock);
        act.Should().Throw<DomainException>()
            .WithMessage("*CreditAmountObrigatorio*");
    }

    [Fact]
    public void Consume_NullIdempotencyKey_ShouldThrow()
    {
        // Arrange
        var wallet = CreateWallet();
        var amount = CreditAmount.Create(50m);

        // Act & Assert
        var act = () => wallet.Consume(amount, null!, "Purpose", _clock);
        act.Should().Throw<DomainException>()
            .WithMessage("*IdempotencyKeyObrigatorio*");
    }

    [Fact]
    public void Consume_InsufficientBalance_ShouldThrow()
    {
        // Arrange
        var wallet = CreateWallet();
        wallet.Grant(CreditAmount.Create(30m), "Bônus", _clock);
        var amount = CreditAmount.Create(50m);
        var idempotencyKey = IdempotencyKey.Generate();

        // Act & Assert
        var act = () => wallet.Consume(amount, idempotencyKey, "Consumo", _clock);
        act.Should().Throw<DomainException>()
            .WithMessage("*SaldoInsuficiente*");
    }

    [Fact]
    public void Consume_WithSameIdempotencyKey_ShouldBeIdempotent()
    {
        // Arrange
        var wallet = CreateWallet();
        wallet.Grant(CreditAmount.Create(100m), "Bônus", _clock);
        var amount = CreditAmount.Create(50m);
        var idempotencyKey = IdempotencyKey.Generate();

        // Act
        wallet.Consume(amount, idempotencyKey, "Consumo 1", _clock);
        var balanceAfterFirst = wallet.Balance;
        wallet.Consume(amount, idempotencyKey, "Consumo 2", _clock);

        // Assert
        wallet.Balance.Should().Be(balanceAfterFirst); // Não deve deduzir novamente
    }

    [Fact]
    public void Refund_NullAmount_ShouldThrow()
    {
        // Arrange
        var wallet = CreateWallet();
        var idempotencyKey = IdempotencyKey.Generate();

        // Act & Assert
        var act = () => wallet.Refund(null!, idempotencyKey, "Purpose", _clock);
        act.Should().Throw<DomainException>()
            .WithMessage("*CreditAmountObrigatorio*");
    }

    [Fact]
    public void Refund_NullIdempotencyKey_ShouldThrow()
    {
        // Arrange
        var wallet = CreateWallet();
        var amount = CreditAmount.Create(50m);

        // Act & Assert
        var act = () => wallet.Refund(amount, null!, "Purpose", _clock);
        act.Should().Throw<DomainException>()
            .WithMessage("*IdempotencyKeyObrigatorio*");
    }

    [Fact]
    public void Refund_WithSameIdempotencyKey_ShouldBeIdempotent()
    {
        // Arrange
        var wallet = CreateWallet();
        wallet.Grant(CreditAmount.Create(100m), "Bônus", _clock);
        var amount = CreditAmount.Create(20m);
        var idempotencyKey = IdempotencyKey.Generate();

        // Act
        wallet.Refund(amount, idempotencyKey, "Refund 1", _clock);
        var balanceAfterFirst = wallet.Balance;
        wallet.Refund(amount, idempotencyKey, "Refund 2", _clock);

        // Assert
        wallet.Balance.Should().Be(balanceAfterFirst); // Não deve adicionar novamente
    }

    [Fact]
    public void Balance_WithMultipleTransactions_ShouldCalculateCorrectly()
    {
        // Arrange
        var wallet = CreateWallet();
        wallet.Grant(CreditAmount.Create(100m), "Grant", _clock);
        wallet.Reserve(CreditAmount.Create(30m), IdempotencyKey.Generate(), "Reserve", _clock);
        wallet.Consume(CreditAmount.Create(20m), IdempotencyKey.Generate(), "Consume", _clock);
        wallet.Refund(CreditAmount.Create(10m), IdempotencyKey.Generate(), "Refund", _clock);

        // Act
        var balance = wallet.Balance;

        // Assert
        // 100 (grant) - 30 (reserve) - 20 (consume) + 10 (refund) = 60
        balance.Should().Be(CreditAmount.Create(60m));
    }

    private CreditWallet CreateWallet()
    {
        var ownerUserId = Guid.NewGuid();
        return new CreditWallet(ownerUserId, _clock);
    }

    private class TestClock : IClock
    {
        public DateTime UtcNow => DateTime.UtcNow;
    }
}

