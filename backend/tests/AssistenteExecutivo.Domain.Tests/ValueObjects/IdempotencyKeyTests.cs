using AssistenteExecutivo.Domain.Exceptions;
using AssistenteExecutivo.Domain.ValueObjects;
using FluentAssertions;

namespace AssistenteExecutivo.Domain.Tests.ValueObjects;

public class IdempotencyKeyTests
{
    [Fact]
    public void Create_ValidKey_ShouldSucceed()
    {
        // Act
        var key = IdempotencyKey.Create("12345678");

        // Assert
        key.Value.Should().Be("12345678");
    }

    [Fact]
    public void Generate_ShouldCreateValidKey()
    {
        // Act
        var key = IdempotencyKey.Generate();

        // Assert
        key.Value.Should().NotBeNullOrEmpty();
        key.Value.Length.Should().BeGreaterThanOrEqualTo(8);
    }

    [Fact]
    public void Create_EmptyKey_ShouldThrow()
    {
        // Act & Assert
        var act = () => IdempotencyKey.Create("");
        act.Should().Throw<DomainException>()
            .WithMessage("*IdempotencyKeyObrigatorio*");
    }

    [Fact]
    public void Create_KeyTooShort_ShouldThrow()
    {
        // Act & Assert
        var act = () => IdempotencyKey.Create("1234567");
        act.Should().Throw<DomainException>()
            .WithMessage("*IdempotencyKeyMinimoCaracteres*");
    }

    [Fact]
    public void Equals_SameKey_ShouldBeEqual()
    {
        // Arrange
        var key1 = IdempotencyKey.Create("12345678");
        var key2 = IdempotencyKey.Create("12345678");

        // Assert
        key1.Should().Be(key2);
    }
}

