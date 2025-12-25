using AssistenteExecutivo.Domain.Exceptions;
using AssistenteExecutivo.Domain.ValueObjects;
using FluentAssertions;

namespace AssistenteExecutivo.Domain.Tests.ValueObjects;

public class PersonNameTests
{
    [Fact]
    public void Create_ValidName_ShouldSucceed()
    {
        // Act
        var name = PersonName.Create("João", "Silva");

        // Assert
        name.FirstName.Should().Be("João");
        name.LastName.Should().Be("Silva");
        name.FullName.Should().Be("João Silva");
    }

    [Fact]
    public void Create_NameWithoutLastName_ShouldSucceed()
    {
        // Act
        var name = PersonName.Create("João");

        // Assert
        name.FirstName.Should().Be("João");
        name.LastName.Should().BeNull();
        name.FullName.Should().Be("João");
    }

    [Fact]
    public void Create_EmptyName_ShouldThrow()
    {
        // Act & Assert
        var act = () => PersonName.Create("");
        act.Should().Throw<DomainException>()
            .WithMessage("*NomeObrigatorio*");
    }

    [Fact]
    public void Create_NameTooShort_ShouldThrow()
    {
        // Act & Assert
        var act = () => PersonName.Create("A");
        act.Should().Throw<DomainException>()
            .WithMessage("*NomeMinimoCaracteres*");
    }
}

