using AssistenteExecutivo.Domain.Entities;
using AssistenteExecutivo.Domain.Enums;

namespace AssistenteExecutivo.Application.Interfaces;

public interface IDraftDocumentRepository
{
    Task<DraftDocument?> GetByIdAsync(Guid draftId, Guid ownerUserId, CancellationToken cancellationToken = default);
    Task<List<DraftDocument>> GetByOwnerIdAsync(Guid ownerUserId, CancellationToken cancellationToken = default);
    Task<List<DraftDocument>> GetByContactIdAsync(Guid contactId, Guid ownerUserId, CancellationToken cancellationToken = default);
    Task<List<DraftDocument>> GetByCompanyIdAsync(Guid companyId, Guid ownerUserId, CancellationToken cancellationToken = default);
    Task<List<DraftDocument>> GetByDocumentTypeAsync(DocumentType documentType, Guid ownerUserId, CancellationToken cancellationToken = default);
    Task<List<DraftDocument>> GetByStatusAsync(DraftStatus status, Guid ownerUserId, CancellationToken cancellationToken = default);
    Task AddAsync(DraftDocument draft, CancellationToken cancellationToken = default);
    Task UpdateAsync(DraftDocument draft, CancellationToken cancellationToken = default);
    Task DeleteAsync(DraftDocument draft, CancellationToken cancellationToken = default);
}







