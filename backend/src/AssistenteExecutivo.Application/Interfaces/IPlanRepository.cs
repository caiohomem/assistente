using AssistenteExecutivo.Domain.Entities;

namespace AssistenteExecutivo.Application.Interfaces;

public interface IPlanRepository
{
    Task<Plan?> GetByIdAsync(Guid planId, CancellationToken cancellationToken = default);
    Task<List<Plan>> GetAllAsync(bool includeInactive = false, CancellationToken cancellationToken = default);
    Task<List<Plan>> GetActiveAsync(CancellationToken cancellationToken = default);
    Task AddAsync(Plan plan, CancellationToken cancellationToken = default);
    Task UpdateAsync(Plan plan, CancellationToken cancellationToken = default);
    Task<bool> ExistsAsync(Guid planId, CancellationToken cancellationToken = default);
}



