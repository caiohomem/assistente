using AssistenteExecutivo.Domain.Entities;

namespace AssistenteExecutivo.Application.Interfaces;

public interface IMilestoneRepository
{
    Task<Milestone?> GetByIdAsync(Guid milestoneId, CancellationToken cancellationToken = default);
    Task<List<Milestone>> ListByAgreementAsync(Guid agreementId, CancellationToken cancellationToken = default);
    Task AddAsync(Milestone milestone, CancellationToken cancellationToken = default);
    Task UpdateAsync(Milestone milestone, CancellationToken cancellationToken = default);
}
