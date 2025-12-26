using AssistenteExecutivo.Application.Interfaces;
using AssistenteExecutivo.Domain.Entities;
using AssistenteExecutivo.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;

namespace AssistenteExecutivo.Infrastructure.Repositories;

public class CompanyRepository : ICompanyRepository
{
    private readonly ApplicationDbContext _context;

    public CompanyRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<Company?> GetByIdAsync(Guid companyId, CancellationToken cancellationToken = default)
    {
        return await _context.Companies
            .FirstOrDefaultAsync(c => c.CompanyId == companyId, cancellationToken);
    }

    public async Task<Company?> GetByNameAsync(string name, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(name))
            return null;

        var normalizedName = name.Trim();

        return await _context.Companies
            .FirstOrDefaultAsync(c => c.Name == normalizedName, cancellationToken);
    }

    public async Task<Company?> GetByDomainAsync(string domain, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(domain))
            return null;

        // Normalize domain (same as Company.AddDomain does)
        var normalizedDomain = domain.Trim().ToLowerInvariant();

        // Como Domains é armazenado como JSON, precisamos fazer uma busca que funcione com JSON
        // Para SQL Server, podemos usar JSON_VALUE ou JSON_CONTAINS
        // Para PostgreSQL, usar operadores JSON
        // Vamos usar uma abordagem que funciona com ambos: buscar todas e filtrar em memória
        // (Para produção, considere usar uma função específica do banco de dados)
        
        var companies = await _context.Companies
            .ToListAsync(cancellationToken);

        return companies.FirstOrDefault(c => c.Domains.Contains(normalizedDomain));
    }

    public async Task AddAsync(Company company, CancellationToken cancellationToken = default)
    {
        await _context.Companies.AddAsync(company, cancellationToken);
    }

    public async Task UpdateAsync(Company company, CancellationToken cancellationToken = default)
    {
        _context.Companies.Update(company);
        await Task.CompletedTask;
    }

    public async Task<bool> ExistsAsync(Guid companyId, CancellationToken cancellationToken = default)
    {
        return await _context.Companies
            .AnyAsync(c => c.CompanyId == companyId, cancellationToken);
    }
}



