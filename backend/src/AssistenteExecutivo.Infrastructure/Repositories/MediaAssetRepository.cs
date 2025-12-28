using AssistenteExecutivo.Application.Interfaces;
using AssistenteExecutivo.Domain.Entities;
using AssistenteExecutivo.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace AssistenteExecutivo.Infrastructure.Repositories;

public class MediaAssetRepository : IMediaAssetRepository
{
    private readonly ApplicationDbContext _context;

    public MediaAssetRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<MediaAsset?> GetByIdAsync(Guid mediaId, Guid ownerUserId, CancellationToken cancellationToken = default)
    {
        return await _context.MediaAssets
            .FirstOrDefaultAsync(m => m.MediaId == mediaId && m.OwnerUserId == ownerUserId, cancellationToken);
    }

    public async Task<MediaAsset?> GetByHashAsync(string hash, Guid ownerUserId, CancellationToken cancellationToken = default)
    {
        return await _context.MediaAssets
            .Where(m => m.OwnerUserId == ownerUserId)
            .FirstOrDefaultAsync(m => m.MediaRef.Hash == hash, cancellationToken);
    }

    public async Task<List<MediaAsset>> GetAllByOwnerAsync(Guid ownerUserId, CancellationToken cancellationToken = default)
    {
        return await _context.MediaAssets
            .Where(m => m.OwnerUserId == ownerUserId)
            .ToListAsync(cancellationToken);
    }

    public async Task AddAsync(MediaAsset mediaAsset, CancellationToken cancellationToken = default)
    {
        await _context.MediaAssets.AddAsync(mediaAsset, cancellationToken);
    }

    public async Task DeleteAsync(MediaAsset mediaAsset, CancellationToken cancellationToken = default)
    {
        _context.MediaAssets.Remove(mediaAsset);
        await Task.CompletedTask;
    }
}










