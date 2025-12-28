using AssistenteExecutivo.Application.Interfaces;
using AssistenteExecutivo.Domain.Entities;
using AssistenteExecutivo.Infrastructure.Repositories;
using FluentAssertions;

namespace AssistenteExecutivo.Infrastructure.Tests.Repositories;

public class CompanyRepositoryTests : RepositoryTestBase
{
    private readonly ICompanyRepository _repository;

    public CompanyRepositoryTests()
    {
        _repository = new CompanyRepository(Context);
    }

    [Fact]
    public async Task GetByIdAsync_ExistingCompany_ShouldReturnCompany()
    {
        // Arrange
        var companyId = Guid.NewGuid();
        var company = new Company(companyId, "Acme Corp", Clock);
        company.AddDomain("acme.com");

        await Context.Companies.AddAsync(company);
        await SaveChangesAsync();

        // Act
        var result = await _repository.GetByIdAsync(companyId);

        // Assert
        result.Should().NotBeNull();
        result!.CompanyId.Should().Be(companyId);
        result.Name.Should().Be("Acme Corp");
    }

    [Fact]
    public async Task GetByIdAsync_NonExistentCompany_ShouldReturnNull()
    {
        // Arrange
        var companyId = Guid.NewGuid();

        // Act
        var result = await _repository.GetByIdAsync(companyId);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task GetByNameAsync_ExistingCompany_ShouldReturnCompany()
    {
        // Arrange
        var company = new Company(Guid.NewGuid(), "Acme Corp", Clock);
        await Context.Companies.AddAsync(company);
        await SaveChangesAsync();

        // Act
        var result = await _repository.GetByNameAsync("Acme Corp");

        // Assert
        result.Should().NotBeNull();
        result!.Name.Should().Be("Acme Corp");
    }

    [Fact]
    public async Task GetByNameAsync_CaseSensitive_ShouldReturnNull()
    {
        // Arrange
        var company = new Company(Guid.NewGuid(), "Acme Corp", Clock);
        await Context.Companies.AddAsync(company);
        await SaveChangesAsync();

        // Act
        var result = await _repository.GetByNameAsync("acme corp");

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task GetByNameAsync_Whitespace_ShouldReturnNull()
    {
        // Arrange
        var company = new Company(Guid.NewGuid(), "Acme Corp", Clock);
        await Context.Companies.AddAsync(company);
        await SaveChangesAsync();

        // Act
        var result = await _repository.GetByNameAsync("   ");

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task GetByDomainAsync_ExistingDomain_ShouldReturnCompany()
    {
        // Arrange
        var company = new Company(Guid.NewGuid(), "Acme Corp", Clock);
        company.AddDomain("acme.com");
        await Context.Companies.AddAsync(company);
        await SaveChangesAsync();

        // Act
        var result = await _repository.GetByDomainAsync("acme.com");

        // Assert
        result.Should().NotBeNull();
        result!.Name.Should().Be("Acme Corp");
    }

    [Fact]
    public async Task GetByDomainAsync_CaseInsensitive_ShouldReturnCompany()
    {
        // Arrange
        var company = new Company(Guid.NewGuid(), "Acme Corp", Clock);
        company.AddDomain("acme.com");
        await Context.Companies.AddAsync(company);
        await SaveChangesAsync();

        // Act
        var result = await _repository.GetByDomainAsync("ACME.COM");

        // Assert
        result.Should().NotBeNull();
        result!.Name.Should().Be("Acme Corp");
    }

    [Fact]
    public async Task GetByDomainAsync_NonExistentDomain_ShouldReturnNull()
    {
        // Arrange
        var company = new Company(Guid.NewGuid(), "Acme Corp", Clock);
        company.AddDomain("acme.com");
        await Context.Companies.AddAsync(company);
        await SaveChangesAsync();

        // Act
        var result = await _repository.GetByDomainAsync("example.com");

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task AddAsync_ShouldAddCompany()
    {
        // Arrange
        var company = new Company(Guid.NewGuid(), "Acme Corp", Clock);

        // Act
        await _repository.AddAsync(company);
        await SaveChangesAsync();

        // Assert
        var result = await Context.Companies.FindAsync(company.CompanyId);
        result.Should().NotBeNull();
        result!.Name.Should().Be("Acme Corp");
    }

    [Fact]
    public async Task UpdateAsync_ShouldUpdateCompany()
    {
        // Arrange
        var company = new Company(Guid.NewGuid(), "Acme Corp", Clock);
        await Context.Companies.AddAsync(company);
        await SaveChangesAsync();

        company.UpdateNotes("Updated notes");

        // Act
        await _repository.UpdateAsync(company);
        await SaveChangesAsync();

        // Assert
        var result = await Context.Companies.FindAsync(company.CompanyId);
        result!.Notes.Should().Be("Updated notes");
    }

    [Fact]
    public async Task ExistsAsync_ExistingCompany_ShouldReturnTrue()
    {
        // Arrange
        var companyId = Guid.NewGuid();
        var company = new Company(companyId, "Acme Corp", Clock);
        await Context.Companies.AddAsync(company);
        await SaveChangesAsync();

        // Act
        var result = await _repository.ExistsAsync(companyId);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public async Task ExistsAsync_NonExistentCompany_ShouldReturnFalse()
    {
        // Arrange
        var companyId = Guid.NewGuid();

        // Act
        var result = await _repository.ExistsAsync(companyId);

        // Assert
        result.Should().BeFalse();
    }
}

