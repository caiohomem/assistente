using AssistenteExecutivo.Domain.Entities;

namespace AssistenteExecutivo.Application.Interfaces;

public interface ICommissionAgreementRepository
{
    Task<CommissionAgreement?> GetByIdAsync(Guid agreementId, CancellationToken cancellationToken = default);
    Task<CommissionAgreement?> GetByPartyIdAsync(Guid partyId, CancellationToken cancellationToken = default);
    Task<List<CommissionAgreement>> ListByOwnerAsync(Guid ownerUserId, CancellationToken cancellationToken = default);
    Task AddAsync(CommissionAgreement agreement, CancellationToken cancellationToken = default);
    Task UpdateAsync(CommissionAgreement agreement, CancellationToken cancellationToken = default);
}
