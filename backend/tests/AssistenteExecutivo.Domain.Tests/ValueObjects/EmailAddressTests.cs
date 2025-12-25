using AssistenteExecutivo.Domain.Exceptions;
using AssistenteExecutivo.Domain.ValueObjects;
using FluentAssertions;

namespace AssistenteExecutivo.Domain.Tests.ValueObjects;

public class EmailAddressTests
{
    [Fact]
    public void Create_ValidEmail_ShouldSucceed()
    {
        // Act
        var email = EmailAddress.Create("test@example.com");

        // Assert
        email.Value.Should().Be("test@example.com");
    }

    [Fact]
    public void Create_EmailWithWhitespace_ShouldNormalize()
    {
        // Act
        var email = EmailAddress.Create("  TEST@EXAMPLE.COM  ");

        // Assert
        email.Value.Should().Be("test@example.com");
    }

    [Fact]
    public void Create_InvalidEmail_ShouldThrow()
    {
        // Act & Assert
        var act = () => EmailAddress.Create("invalid-email");
        act.Should().Throw<DomainException>()
            .WithMessage("*EmailInvalido*");
    }

    [Fact]
    public void Create_EmptyEmail_ShouldThrow()
    {
        // Act & Assert
        var act = () => EmailAddress.Create("");
        act.Should().Throw<DomainException>();
    }

    [Fact]
    public void Equals_SameEmail_ShouldBeEqual()
    {
        // Arrange
        var email1 = EmailAddress.Create("test@example.com");
        var email2 = EmailAddress.Create("TEST@EXAMPLE.COM");

        // Assert
        email1.Should().Be(email2);
        (email1 == email2).Should().BeTrue();
    }

    [Fact]
    public void Equals_DifferentEmail_ShouldNotBeEqual()
    {
        // Arrange
        var email1 = EmailAddress.Create("test1@example.com");
        var email2 = EmailAddress.Create("test2@example.com");

        // Assert
        email1.Should().NotBe(email2);
        (email1 != email2).Should().BeTrue();
    }
}

