using AssistenteExecutivo.Domain.Entities;

namespace AssistenteExecutivo.Application.Interfaces;

public interface IMediaAssetRepository
{
    Task<MediaAsset?> GetByIdAsync(Guid mediaId, Guid ownerUserId, CancellationToken cancellationToken = default);
    Task<MediaAsset?> GetByHashAsync(string hash, Guid ownerUserId, CancellationToken cancellationToken = default);
    Task<List<MediaAsset>> GetAllByOwnerAsync(Guid ownerUserId, CancellationToken cancellationToken = default);
    Task AddAsync(MediaAsset mediaAsset, CancellationToken cancellationToken = default);
    Task DeleteAsync(MediaAsset mediaAsset, CancellationToken cancellationToken = default);
}






