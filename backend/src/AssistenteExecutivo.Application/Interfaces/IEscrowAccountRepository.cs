using AssistenteExecutivo.Domain.Entities;

namespace AssistenteExecutivo.Application.Interfaces;

public interface IEscrowAccountRepository
{
    Task<EscrowAccount?> GetByIdAsync(Guid escrowAccountId, CancellationToken cancellationToken = default);
    Task<EscrowAccount?> GetByAgreementIdAsync(Guid agreementId, CancellationToken cancellationToken = default);
    Task<List<EscrowTransaction>> ListTransactionsAsync(Guid escrowAccountId, CancellationToken cancellationToken = default);
    Task AddAsync(EscrowAccount escrowAccount, CancellationToken cancellationToken = default);
    Task UpdateAsync(EscrowAccount escrowAccount, CancellationToken cancellationToken = default);
}
