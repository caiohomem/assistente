using AssistenteExecutivo.Domain.Entities;
using AssistenteExecutivo.Domain.Exceptions;
using AssistenteExecutivo.Domain.Interfaces;
using FluentAssertions;

namespace AssistenteExecutivo.Domain.Tests.Entities;

public class CompanyTests
{
    private readonly IClock _clock;

    public CompanyTests()
    {
        _clock = new TestClock();
    }

    [Fact]
    public void Constructor_ValidData_ShouldCreate()
    {
        // Arrange
        var companyId = Guid.NewGuid();
        var name = "Empresa XYZ";

        // Act
        var company = new Company(companyId, name, _clock);

        // Assert
        company.CompanyId.Should().Be(companyId);
        company.Name.Should().Be(name);
        company.Domains.Should().BeEmpty();
        company.Notes.Should().BeNull();
    }

    [Fact]
    public void Constructor_EmptyName_ShouldThrow()
    {
        // Arrange
        var companyId = Guid.NewGuid();

        // Act & Assert
        var act = () => new Company(companyId, "", _clock);
        act.Should().Throw<DomainException>()
            .WithMessage("*NomeEmpresaObrigatorio*");
    }

    [Fact]
    public void AddDomain_ShouldAddDomain()
    {
        // Arrange
        var company = CreateCompany();

        // Act
        company.AddDomain("example.com");

        // Assert
        company.Domains.Should().Contain("example.com");
    }

    [Fact]
    public void AddDomain_DuplicateDomain_ShouldNotAddAgain()
    {
        // Arrange
        var company = CreateCompany();
        company.AddDomain("example.com");

        // Act
        company.AddDomain("EXAMPLE.COM");

        // Assert
        company.Domains.Should().ContainSingle(d => d == "example.com");
    }

    [Fact]
    public void AddDomain_EmptyDomain_ShouldThrow()
    {
        // Arrange
        var company = CreateCompany();

        // Act & Assert
        var act = () => company.AddDomain("");
        act.Should().Throw<DomainException>()
            .WithMessage("*DomainObrigatorio*");
    }

    [Fact]
    public void UpdateNotes_ShouldUpdateNotes()
    {
        // Arrange
        var company = CreateCompany();

        // Act
        company.UpdateNotes("Notas importantes");

        // Assert
        company.Notes.Should().Be("Notas importantes");
    }

    [Fact]
    public void UpdateNotes_NullNotes_ShouldSetToNull()
    {
        // Arrange
        var company = CreateCompany();
        company.UpdateNotes("Notas");

        // Act
        company.UpdateNotes(null);

        // Assert
        company.Notes.Should().BeNull();
    }

    private Company CreateCompany()
    {
        var companyId = Guid.NewGuid();
        return new Company(companyId, "Empresa XYZ", _clock);
    }

    private class TestClock : IClock
    {
        public DateTime UtcNow => DateTime.UtcNow;
    }
}

