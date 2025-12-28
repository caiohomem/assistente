using AssistenteExecutivo.Application.Interfaces;
using AssistenteExecutivo.Domain.Entities;
using AssistenteExecutivo.Domain.Enums;
using AssistenteExecutivo.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace AssistenteExecutivo.Infrastructure.Repositories;

public class CaptureJobRepository : ICaptureJobRepository
{
    private readonly ApplicationDbContext _context;

    public CaptureJobRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<CaptureJob?> GetByIdAsync(Guid jobId, Guid ownerUserId, CancellationToken cancellationToken = default)
    {
        return await _context.CaptureJobs
            .FirstOrDefaultAsync(j => j.JobId == jobId && j.OwnerUserId == ownerUserId, cancellationToken);
    }

    public async Task<List<CaptureJob>> GetAllByOwnerUserIdAsync(Guid ownerUserId, CancellationToken cancellationToken = default)
    {
        return await _context.CaptureJobs
            .Where(j => j.OwnerUserId == ownerUserId)
            .OrderByDescending(j => j.RequestedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task<List<CaptureJob>> GetByStatusAsync(JobStatus status, Guid ownerUserId, CancellationToken cancellationToken = default)
    {
        return await _context.CaptureJobs
            .Where(j => j.Status == status && j.OwnerUserId == ownerUserId)
            .ToListAsync(cancellationToken);
    }

    public async Task<List<CaptureJob>> GetByContactIdAsync(Guid contactId, Guid ownerUserId, CancellationToken cancellationToken = default)
    {
        return await _context.CaptureJobs
            .Where(j => j.ContactId == contactId && j.OwnerUserId == ownerUserId)
            .ToListAsync(cancellationToken);
    }

    public async Task<List<CaptureJob>> GetByMediaIdAsync(Guid mediaId, Guid ownerUserId, CancellationToken cancellationToken = default)
    {
        return await _context.CaptureJobs
            .Where(j => j.MediaId == mediaId && j.OwnerUserId == ownerUserId)
            .ToListAsync(cancellationToken);
    }

    public async Task<CaptureJob?> GetLatestAudioJobByContactIdAsync(Guid contactId, Guid ownerUserId, CancellationToken cancellationToken = default)
    {
        return await _context.CaptureJobs
            .Where(j => j.ContactId == contactId
                && j.OwnerUserId == ownerUserId
                && j.Type == JobType.AudioNoteTranscription)
            .OrderByDescending(j => j.RequestedAt)
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<CaptureJob?> GetAudioJobByContactIdAndDateAsync(Guid contactId, Guid ownerUserId, DateTime noteCreatedAt, CancellationToken cancellationToken = default)
    {
        // Buscar o CaptureJob de áudio que foi completado antes ou na mesma data da criação da nota
        // e que seja o mais próximo possível da data da nota
        return await _context.CaptureJobs
            .Where(j => j.ContactId == contactId
                && j.OwnerUserId == ownerUserId
                && j.Type == JobType.AudioNoteTranscription
                && j.CompletedAt.HasValue
                && j.CompletedAt.Value <= noteCreatedAt.AddSeconds(5)) // Tolerância de 5 segundos
            .OrderByDescending(j => j.CompletedAt)
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task AddAsync(CaptureJob job, CancellationToken cancellationToken = default)
    {
        await _context.CaptureJobs.AddAsync(job, cancellationToken);
    }

    public async Task UpdateAsync(CaptureJob job, CancellationToken cancellationToken = default)
    {
        var entry = _context.Entry(job);

        // Se o job ja esta sendo rastreado como novo, nao force Modified
        if (entry.State == EntityState.Added)
            return;

        // Se o job nao estiver sendo rastreado, anexa para aplicar as mudancas
        if (entry.State == EntityState.Detached)
        {
            _context.CaptureJobs.Update(job);
        }

        await Task.CompletedTask;
    }
}
