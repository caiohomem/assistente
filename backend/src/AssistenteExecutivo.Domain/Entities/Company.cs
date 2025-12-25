using AssistenteExecutivo.Domain.Exceptions;
using AssistenteExecutivo.Domain.Interfaces;

namespace AssistenteExecutivo.Domain.Entities;

public class Company
{
    private readonly List<string> _domains = new();

    private Company() { } // EF Core

    public Company(
        Guid companyId,
        string name,
        IClock clock)
    {
        if (companyId == Guid.Empty)
            throw new DomainException("Domain:CompanyIdObrigatorio");

        if (string.IsNullOrWhiteSpace(name))
            throw new DomainException("Domain:NomeEmpresaObrigatorio");

        if (clock == null)
            throw new DomainException("Domain:ClockObrigatorio");

        CompanyId = companyId;
        Name = name.Trim();
        CreatedAt = clock.UtcNow;
    }

    public Guid CompanyId { get; private set; }
    public string Name { get; private set; } = null!;
    public IReadOnlyCollection<string> Domains => _domains.AsReadOnly();
    public string? Notes { get; private set; }
    public DateTime CreatedAt { get; private set; }

    public void AddDomain(string domain)
    {
        if (string.IsNullOrWhiteSpace(domain))
            throw new DomainException("Domain:DomainObrigatorio");

        var normalizedDomain = domain.Trim().ToLowerInvariant();
        if (!_domains.Contains(normalizedDomain))
        {
            _domains.Add(normalizedDomain);
        }
    }

    public void UpdateNotes(string? notes)
    {
        Notes = notes?.Trim();
    }
}

