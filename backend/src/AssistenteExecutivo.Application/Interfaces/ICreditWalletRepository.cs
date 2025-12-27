using AssistenteExecutivo.Domain.Entities;

namespace AssistenteExecutivo.Application.Interfaces;

public interface ICreditWalletRepository
{
    Task<CreditWallet?> GetByOwnerIdAsync(Guid ownerUserId, CancellationToken cancellationToken = default);
    Task AddAsync(CreditWallet wallet, CancellationToken cancellationToken = default);
    Task UpdateAsync(CreditWallet wallet, CancellationToken cancellationToken = default);
    Task<CreditWallet> GetOrCreateAsync(Guid ownerUserId, CancellationToken cancellationToken = default);
}





