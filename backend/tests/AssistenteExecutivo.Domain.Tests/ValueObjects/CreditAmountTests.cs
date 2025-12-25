using AssistenteExecutivo.Domain.Exceptions;
using AssistenteExecutivo.Domain.ValueObjects;
using FluentAssertions;

namespace AssistenteExecutivo.Domain.Tests.ValueObjects;

public class CreditAmountTests
{
    [Fact]
    public void Create_ValidAmount_ShouldSucceed()
    {
        // Act
        var amount = CreditAmount.Create(100.50m);

        // Assert
        amount.Value.Should().Be(100.50m);
    }

    [Fact]
    public void Create_NegativeAmount_ShouldThrow()
    {
        // Act & Assert
        var act = () => CreditAmount.Create(-10m);
        act.Should().Throw<DomainException>()
            .WithMessage("*CreditAmountNaoPodeSerNegativo*");
    }

    [Fact]
    public void Zero_ShouldReturnZero()
    {
        // Act
        var zero = CreditAmount.Zero;

        // Assert
        zero.Value.Should().Be(0m);
    }

    [Fact]
    public void OperatorPlus_ShouldAddAmounts()
    {
        // Arrange
        var amount1 = CreditAmount.Create(50m);
        var amount2 = CreditAmount.Create(30m);

        // Act
        var result = amount1 + amount2;

        // Assert
        result.Value.Should().Be(80m);
    }

    [Fact]
    public void OperatorMinus_ValidSubtraction_ShouldSubtract()
    {
        // Arrange
        var amount1 = CreditAmount.Create(50m);
        var amount2 = CreditAmount.Create(30m);

        // Act
        var result = amount1 - amount2;

        // Assert
        result.Value.Should().Be(20m);
    }

    [Fact]
    public void OperatorMinus_NegativeResult_ShouldThrow()
    {
        // Arrange
        var amount1 = CreditAmount.Create(30m);
        var amount2 = CreditAmount.Create(50m);

        // Act & Assert
        var act = () => amount1 - amount2;
        act.Should().Throw<DomainException>()
            .WithMessage("*CreditAmountResultadoNegativo*");
    }
}

