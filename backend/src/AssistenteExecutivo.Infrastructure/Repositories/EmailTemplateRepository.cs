using AssistenteExecutivo.Application.Interfaces;
using AssistenteExecutivo.Domain.Notifications;
using AssistenteExecutivo.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace AssistenteExecutivo.Infrastructure.Repositories;

public class EmailTemplateRepository : IEmailTemplateRepository
{
    private readonly ApplicationDbContext _context;

    public EmailTemplateRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<EmailTemplate?> GetByTypeAsync(EmailTemplateType templateType, CancellationToken cancellationToken = default)
    {
        return await _context.EmailTemplates
            .FirstOrDefaultAsync(t => t.TemplateType == templateType && t.IsActive, cancellationToken);
    }

    public async Task<EmailTemplate?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _context.EmailTemplates.FindAsync(new object[] { id }, cancellationToken);
    }

    public async Task<List<EmailTemplate>> GetAllActiveAsync(CancellationToken cancellationToken = default)
    {
        return await _context.EmailTemplates
            .Where(t => t.IsActive)
            .ToListAsync(cancellationToken);
    }

    public async Task<List<EmailTemplate>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        return await _context.EmailTemplates
            .ToListAsync(cancellationToken);
    }

    public async Task<List<EmailTemplate>> GetByTypeFilterAsync(EmailTemplateType? templateType, bool? activeOnly, CancellationToken cancellationToken = default)
    {
        var query = _context.EmailTemplates.AsQueryable();

        if (templateType.HasValue)
        {
            query = query.Where(t => t.TemplateType == templateType.Value);
        }

        if (activeOnly == true)
        {
            query = query.Where(t => t.IsActive);
        }

        return await query.ToListAsync(cancellationToken);
    }

    public async Task AddAsync(EmailTemplate template, CancellationToken cancellationToken = default)
    {
        await _context.EmailTemplates.AddAsync(template, cancellationToken);
    }

    public async Task UpdateAsync(EmailTemplate template, CancellationToken cancellationToken = default)
    {
        _context.EmailTemplates.Update(template);
        await Task.CompletedTask;
    }

    public async Task DeleteAsync(EmailTemplate template, CancellationToken cancellationToken = default)
    {
        _context.EmailTemplates.Remove(template);
        await Task.CompletedTask;
    }
}

