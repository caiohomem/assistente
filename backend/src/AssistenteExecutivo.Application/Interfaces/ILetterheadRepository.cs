using AssistenteExecutivo.Domain.Entities;

namespace AssistenteExecutivo.Application.Interfaces;

public interface ILetterheadRepository
{
    Task<Letterhead?> GetByIdAsync(Guid letterheadId, Guid ownerUserId, CancellationToken cancellationToken = default);
    Task<List<Letterhead>> GetByOwnerIdAsync(Guid ownerUserId, CancellationToken cancellationToken = default);
    Task<List<Letterhead>> GetActiveByOwnerIdAsync(Guid ownerUserId, CancellationToken cancellationToken = default);
    Task AddAsync(Letterhead letterhead, CancellationToken cancellationToken = default);
    Task UpdateAsync(Letterhead letterhead, CancellationToken cancellationToken = default);
    Task DeleteAsync(Letterhead letterhead, CancellationToken cancellationToken = default);
}





