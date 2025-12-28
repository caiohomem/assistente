using AssistenteExecutivo.Application.Interfaces;
using AssistenteExecutivo.Domain.Entities;
using AssistenteExecutivo.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace AssistenteExecutivo.Infrastructure.Persistence.Repositories;

public class DraftDocumentRepository : IDraftDocumentRepository
{
    private readonly ApplicationDbContext _context;

    public DraftDocumentRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<DraftDocument?> GetByIdAsync(Guid draftId, Guid ownerUserId, CancellationToken cancellationToken = default)
    {
        return await _context.DraftDocuments
            .FirstOrDefaultAsync(d => d.DraftId == draftId && d.OwnerUserId == ownerUserId, cancellationToken);
    }

    public async Task<List<DraftDocument>> GetByOwnerIdAsync(Guid ownerUserId, CancellationToken cancellationToken = default)
    {
        return await _context.DraftDocuments
            .Where(d => d.OwnerUserId == ownerUserId)
            .ToListAsync(cancellationToken);
    }

    public async Task<List<DraftDocument>> GetByContactIdAsync(Guid contactId, Guid ownerUserId, CancellationToken cancellationToken = default)
    {
        return await _context.DraftDocuments
            .Where(d => d.ContactId == contactId && d.OwnerUserId == ownerUserId)
            .ToListAsync(cancellationToken);
    }

    public async Task<List<DraftDocument>> GetByCompanyIdAsync(Guid companyId, Guid ownerUserId, CancellationToken cancellationToken = default)
    {
        return await _context.DraftDocuments
            .Where(d => d.CompanyId == companyId && d.OwnerUserId == ownerUserId)
            .ToListAsync(cancellationToken);
    }

    public async Task<List<DraftDocument>> GetByDocumentTypeAsync(DocumentType documentType, Guid ownerUserId, CancellationToken cancellationToken = default)
    {
        return await _context.DraftDocuments
            .Where(d => d.DocumentType == documentType && d.OwnerUserId == ownerUserId)
            .ToListAsync(cancellationToken);
    }

    public async Task<List<DraftDocument>> GetByStatusAsync(DraftStatus status, Guid ownerUserId, CancellationToken cancellationToken = default)
    {
        return await _context.DraftDocuments
            .Where(d => d.Status == status && d.OwnerUserId == ownerUserId)
            .ToListAsync(cancellationToken);
    }

    public async Task AddAsync(DraftDocument draft, CancellationToken cancellationToken = default)
    {
        await _context.DraftDocuments.AddAsync(draft, cancellationToken);
    }

    public Task UpdateAsync(DraftDocument draft, CancellationToken cancellationToken = default)
    {
        _context.DraftDocuments.Update(draft);
        return Task.CompletedTask;
    }

    public Task DeleteAsync(DraftDocument draft, CancellationToken cancellationToken = default)
    {
        _context.DraftDocuments.Remove(draft);
        return Task.CompletedTask;
    }
}





