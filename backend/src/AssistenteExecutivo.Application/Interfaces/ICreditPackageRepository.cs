using AssistenteExecutivo.Domain.Entities;

namespace AssistenteExecutivo.Application.Interfaces;

public interface ICreditPackageRepository
{
    Task<CreditPackage?> GetByIdAsync(Guid packageId, CancellationToken cancellationToken = default);
    Task<List<CreditPackage>> GetAllAsync(bool includeInactive = false, CancellationToken cancellationToken = default);
    Task<List<CreditPackage>> GetActiveAsync(CancellationToken cancellationToken = default);
    Task AddAsync(CreditPackage package, CancellationToken cancellationToken = default);
    Task UpdateAsync(CreditPackage package, CancellationToken cancellationToken = default);
    Task<bool> ExistsAsync(Guid packageId, CancellationToken cancellationToken = default);
}








