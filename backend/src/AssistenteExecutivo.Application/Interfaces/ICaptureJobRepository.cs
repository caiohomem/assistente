using AssistenteExecutivo.Domain.Entities;
using AssistenteExecutivo.Domain.Enums;

namespace AssistenteExecutivo.Application.Interfaces;

public interface ICaptureJobRepository
{
    Task<CaptureJob?> GetByIdAsync(Guid jobId, Guid ownerUserId, CancellationToken cancellationToken = default);
    Task<List<CaptureJob>> GetAllByOwnerUserIdAsync(Guid ownerUserId, CancellationToken cancellationToken = default);
    Task<List<CaptureJob>> GetByStatusAsync(JobStatus status, Guid ownerUserId, CancellationToken cancellationToken = default);
    Task<List<CaptureJob>> GetByContactIdAsync(Guid contactId, Guid ownerUserId, CancellationToken cancellationToken = default);
    Task<List<CaptureJob>> GetByMediaIdAsync(Guid mediaId, Guid ownerUserId, CancellationToken cancellationToken = default);
    Task<CaptureJob?> GetLatestAudioJobByContactIdAsync(Guid contactId, Guid ownerUserId, CancellationToken cancellationToken = default);
    Task<CaptureJob?> GetAudioJobByContactIdAndDateAsync(Guid contactId, Guid ownerUserId, DateTime noteCreatedAt, CancellationToken cancellationToken = default);
    Task AddAsync(CaptureJob job, CancellationToken cancellationToken = default);
    Task UpdateAsync(CaptureJob job, CancellationToken cancellationToken = default);
}

