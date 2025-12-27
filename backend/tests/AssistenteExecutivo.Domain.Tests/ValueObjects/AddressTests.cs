using AssistenteExecutivo.Domain.ValueObjects;
using FluentAssertions;

namespace AssistenteExecutivo.Domain.Tests.ValueObjects;

public class AddressTests
{
    [Fact]
    public void Create_ValidAddress_ShouldSucceed()
    {
        // Act
        var address = Address.Create(
            street: "Rua das Flores, 123",
            city: "São Paulo",
            state: "SP",
            zipCode: "01234-567",
            country: "Brasil");

        // Assert
        address.Street.Should().Be("Rua das Flores, 123");
        address.City.Should().Be("São Paulo");
        address.State.Should().Be("SP");
        address.ZipCode.Should().Be("01234-567");
        address.Country.Should().Be("Brasil");
        address.IsEmpty.Should().BeFalse();
    }

    [Fact]
    public void Create_PartialAddress_ShouldSucceed()
    {
        // Act
        var address = Address.Create(city: "São Paulo", state: "SP");

        // Assert
        address.City.Should().Be("São Paulo");
        address.State.Should().Be("SP");
        address.Street.Should().BeNull();
    }

    [Fact]
    public void Empty_ShouldReturnEmptyAddress()
    {
        // Act
        var address = Address.Empty;

        // Assert
        address.IsEmpty.Should().BeTrue();
        address.Street.Should().BeNull();
        address.City.Should().BeNull();
    }

    [Fact]
    public void Equals_SameAddress_ShouldBeEqual()
    {
        // Arrange
        var address1 = Address.Create("Rua A", "São Paulo", "SP");
        var address2 = Address.Create("Rua A", "São Paulo", "SP");

        // Assert
        address1.Should().Be(address2);
    }

    [Fact]
    public void Equals_DifferentAddress_ShouldNotBeEqual()
    {
        // Arrange
        var address1 = Address.Create("Rua A", "São Paulo", "SP");
        var address2 = Address.Create("Rua B", "Rio de Janeiro", "RJ");

        // Assert
        address1.Should().NotBe(address2);
    }
}





