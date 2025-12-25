using AssistenteExecutivo.Domain.Exceptions;
using AssistenteExecutivo.Domain.ValueObjects;
using FluentAssertions;

namespace AssistenteExecutivo.Domain.Tests.ValueObjects;

public class KeycloakSubjectTests
{
    [Fact]
    public void Create_ValidSubject_ShouldSucceed()
    {
        // Act
        var subject = KeycloakSubject.Create("sub-123");

        // Assert
        subject.Value.Should().Be("sub-123");
    }

    [Fact]
    public void Create_EmptySubject_ShouldThrow()
    {
        // Act & Assert
        var act = () => KeycloakSubject.Create("");
        act.Should().Throw<DomainException>()
            .WithMessage("*KeycloakSubjectObrigatorio*");
    }

    [Fact]
    public void Equals_SameSubject_ShouldBeEqual()
    {
        // Arrange
        var subject1 = KeycloakSubject.Create("sub-123");
        var subject2 = KeycloakSubject.Create("sub-123");

        // Assert
        subject1.Should().Be(subject2);
    }
}

