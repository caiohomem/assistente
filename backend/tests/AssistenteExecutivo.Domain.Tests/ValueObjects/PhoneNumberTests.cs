using AssistenteExecutivo.Domain.Exceptions;
using AssistenteExecutivo.Domain.ValueObjects;
using FluentAssertions;

namespace AssistenteExecutivo.Domain.Tests.ValueObjects;

public class PhoneNumberTests
{
    [Fact]
    public void Create_ValidPhone10Digits_ShouldSucceed()
    {
        // Act
        var phone = PhoneNumber.Create("1198765432");

        // Assert
        phone.Number.Should().Be("1198765432");
        phone.FormattedNumber.Should().Be("(11) 9876-5432");
    }

    [Fact]
    public void Create_ValidPhone11Digits_ShouldSucceed()
    {
        // Act
        var phone = PhoneNumber.Create("11987654321");

        // Assert
        phone.Number.Should().Be("11987654321");
        phone.FormattedNumber.Should().Be("(11) 98765-4321");
    }

    [Fact]
    public void Create_PhoneWithFormatting_ShouldRemoveFormatting()
    {
        // Act
        var phone = PhoneNumber.Create("(11) 98765-4321");

        // Assert
        phone.Number.Should().Be("11987654321");
    }

    [Fact]
    public void Create_InvalidPhone_ShouldThrow()
    {
        // Act & Assert
        var act = () => PhoneNumber.Create("123");
        act.Should().Throw<DomainException>()
            .WithMessage("*TelefoneInvalido*");
    }

    [Fact]
    public void Equals_SamePhone_ShouldBeEqual()
    {
        // Arrange
        var phone1 = PhoneNumber.Create("11987654321");
        var phone2 = PhoneNumber.Create("(11) 98765-4321");

        // Assert
        phone1.Should().Be(phone2);
    }
}

