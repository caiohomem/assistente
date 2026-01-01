using AssistenteExecutivo.Application.Interfaces;
using AssistenteExecutivo.Domain.Entities;
using AssistenteExecutivo.Domain.Enums;
using AssistenteExecutivo.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace AssistenteExecutivo.Infrastructure.Repositories;

public class WorkflowRepository : IWorkflowRepository
{
    private readonly ApplicationDbContext _context;

    public WorkflowRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<Workflow?> GetByIdAsync(Guid workflowId, CancellationToken cancellationToken = default)
    {
        return await _context.Workflows
            .FirstOrDefaultAsync(w => w.WorkflowId == workflowId, cancellationToken);
    }

    public async Task<Workflow?> GetByIdAndOwnerAsync(Guid workflowId, Guid ownerUserId, CancellationToken cancellationToken = default)
    {
        return await _context.Workflows
            .FirstOrDefaultAsync(w => w.WorkflowId == workflowId && w.OwnerUserId == ownerUserId, cancellationToken);
    }

    public async Task<Workflow?> GetByN8nWorkflowIdAsync(string n8nWorkflowId, CancellationToken cancellationToken = default)
    {
        return await _context.Workflows
            .FirstOrDefaultAsync(w => w.N8nWorkflowId == n8nWorkflowId, cancellationToken);
    }

    public async Task<Workflow?> GetByIdempotencyKeyAsync(string idempotencyKey, CancellationToken cancellationToken = default)
    {
        return await _context.Workflows
            .FirstOrDefaultAsync(w => w.IdempotencyKey == idempotencyKey, cancellationToken);
    }

    public async Task<List<Workflow>> GetByOwnerAsync(Guid ownerUserId, CancellationToken cancellationToken = default)
    {
        return await _context.Workflows
            .Where(w => w.OwnerUserId == ownerUserId && w.Status != WorkflowStatus.Archived)
            .OrderByDescending(w => w.UpdatedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task<List<Workflow>> GetByOwnerAndStatusAsync(Guid ownerUserId, WorkflowStatus status, CancellationToken cancellationToken = default)
    {
        return await _context.Workflows
            .Where(w => w.OwnerUserId == ownerUserId && w.Status == status)
            .OrderByDescending(w => w.UpdatedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task<bool> ExistsByNameAndOwnerAsync(string name, Guid ownerUserId, CancellationToken cancellationToken = default)
    {
        return await _context.Workflows
            .AnyAsync(w => w.Name == name && w.OwnerUserId == ownerUserId && w.Status != WorkflowStatus.Archived, cancellationToken);
    }

    public async Task AddAsync(Workflow workflow, CancellationToken cancellationToken = default)
    {
        await _context.Workflows.AddAsync(workflow, cancellationToken);
    }

    public Task UpdateAsync(Workflow workflow, CancellationToken cancellationToken = default)
    {
        _context.Workflows.Update(workflow);
        return Task.CompletedTask;
    }

    public Task DeleteAsync(Workflow workflow, CancellationToken cancellationToken = default)
    {
        _context.Workflows.Remove(workflow);
        return Task.CompletedTask;
    }
}
