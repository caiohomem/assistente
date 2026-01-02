using AssistenteExecutivo.Application.Interfaces;
using AssistenteExecutivo.Domain.Entities;
using AssistenteExecutivo.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace AssistenteExecutivo.Infrastructure.Persistence.Repositories;

public class TemplateRepository : ITemplateRepository
{
    private readonly ApplicationDbContext _context;

    public TemplateRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<Template?> GetByIdAsync(Guid templateId, Guid ownerUserId, CancellationToken cancellationToken = default)
    {
        return await _context.Templates
            .FirstOrDefaultAsync(t => t.TemplateId == templateId && t.OwnerUserId == ownerUserId, cancellationToken);
    }

    public async Task<List<Template>> GetByOwnerIdAsync(Guid ownerUserId, CancellationToken cancellationToken = default)
    {
        return await _context.Templates
            .Where(t => t.OwnerUserId == ownerUserId)
            .ToListAsync(cancellationToken);
    }

    public async Task<List<Template>> GetByTypeAsync(TemplateType type, Guid ownerUserId, CancellationToken cancellationToken = default)
    {
        return await _context.Templates
            .Where(t => t.Type == type && t.OwnerUserId == ownerUserId)
            .ToListAsync(cancellationToken);
    }

    public async Task<List<Template>> GetActiveByOwnerIdAsync(Guid ownerUserId, CancellationToken cancellationToken = default)
    {
        return await _context.Templates
            .Where(t => t.OwnerUserId == ownerUserId && t.Active)
            .ToListAsync(cancellationToken);
    }

    public async Task<List<Template>> GetActiveByTypeAsync(TemplateType type, Guid ownerUserId, CancellationToken cancellationToken = default)
    {
        return await _context.Templates
            .Where(t => t.Type == type && t.OwnerUserId == ownerUserId && t.Active)
            .ToListAsync(cancellationToken);
    }

    public async Task AddAsync(Template template, CancellationToken cancellationToken = default)
    {
        await _context.Templates.AddAsync(template, cancellationToken);
    }

    public Task UpdateAsync(Template template, CancellationToken cancellationToken = default)
    {
        _context.Templates.Update(template);
        return Task.CompletedTask;
    }

    public Task DeleteAsync(Template template, CancellationToken cancellationToken = default)
    {
        _context.Templates.Remove(template);
        return Task.CompletedTask;
    }
}









