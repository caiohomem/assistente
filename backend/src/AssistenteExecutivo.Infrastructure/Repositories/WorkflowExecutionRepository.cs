using AssistenteExecutivo.Application.Interfaces;
using AssistenteExecutivo.Domain.Entities;
using AssistenteExecutivo.Domain.Enums;
using AssistenteExecutivo.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace AssistenteExecutivo.Infrastructure.Repositories;

public class WorkflowExecutionRepository : IWorkflowExecutionRepository
{
    private readonly ApplicationDbContext _context;

    public WorkflowExecutionRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<WorkflowExecution?> GetByIdAsync(Guid executionId, CancellationToken cancellationToken = default)
    {
        return await _context.WorkflowExecutions
            .FirstOrDefaultAsync(e => e.ExecutionId == executionId, cancellationToken);
    }

    public async Task<WorkflowExecution?> GetByIdAndOwnerAsync(Guid executionId, Guid ownerUserId, CancellationToken cancellationToken = default)
    {
        return await _context.WorkflowExecutions
            .FirstOrDefaultAsync(e => e.ExecutionId == executionId && e.OwnerUserId == ownerUserId, cancellationToken);
    }

    public async Task<WorkflowExecution?> GetByN8nExecutionIdAsync(string n8nExecutionId, CancellationToken cancellationToken = default)
    {
        return await _context.WorkflowExecutions
            .FirstOrDefaultAsync(e => e.N8nExecutionId == n8nExecutionId, cancellationToken);
    }

    public async Task<WorkflowExecution?> GetByIdempotencyKeyAsync(string idempotencyKey, CancellationToken cancellationToken = default)
    {
        return await _context.WorkflowExecutions
            .FirstOrDefaultAsync(e => e.IdempotencyKey == idempotencyKey, cancellationToken);
    }

    public async Task<List<WorkflowExecution>> GetByWorkflowIdAsync(Guid workflowId, CancellationToken cancellationToken = default)
    {
        return await _context.WorkflowExecutions
            .Where(e => e.WorkflowId == workflowId)
            .OrderByDescending(e => e.StartedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task<List<WorkflowExecution>> GetByOwnerAsync(Guid ownerUserId, int limit = 50, CancellationToken cancellationToken = default)
    {
        return await _context.WorkflowExecutions
            .Where(e => e.OwnerUserId == ownerUserId)
            .OrderByDescending(e => e.StartedAt)
            .Take(limit)
            .ToListAsync(cancellationToken);
    }

    public async Task<List<WorkflowExecution>> GetByStatusAsync(WorkflowExecutionStatus status, CancellationToken cancellationToken = default)
    {
        return await _context.WorkflowExecutions
            .Where(e => e.Status == status)
            .OrderByDescending(e => e.StartedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task<List<WorkflowExecution>> GetPendingApprovalsAsync(Guid ownerUserId, CancellationToken cancellationToken = default)
    {
        return await _context.WorkflowExecutions
            .Where(e => e.OwnerUserId == ownerUserId && e.Status == WorkflowExecutionStatus.WaitingApproval)
            .OrderByDescending(e => e.StartedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task AddAsync(WorkflowExecution execution, CancellationToken cancellationToken = default)
    {
        await _context.WorkflowExecutions.AddAsync(execution, cancellationToken);
    }

    public Task UpdateAsync(WorkflowExecution execution, CancellationToken cancellationToken = default)
    {
        _context.WorkflowExecutions.Update(execution);
        return Task.CompletedTask;
    }
}
