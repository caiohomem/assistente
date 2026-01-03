using AssistenteExecutivo.Domain.Entities;

namespace AssistenteExecutivo.Application.Interfaces;

public interface INegotiationSessionRepository
{
    Task<NegotiationSession?> GetByIdAsync(Guid sessionId, CancellationToken cancellationToken = default);
    Task<List<NegotiationSession>> ListByOwnerAsync(Guid ownerUserId, CancellationToken cancellationToken = default);
    Task<List<NegotiationSession>> ListByAgreementAsync(Guid agreementId, CancellationToken cancellationToken = default);
    Task AddAsync(NegotiationSession session, CancellationToken cancellationToken = default);
    Task UpdateAsync(NegotiationSession session, CancellationToken cancellationToken = default);
}
