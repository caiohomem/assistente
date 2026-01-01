using AssistenteExecutivo.Domain.Entities;
using AssistenteExecutivo.Domain.Enums;

namespace AssistenteExecutivo.Application.Interfaces;

public interface IWorkflowExecutionRepository
{
    Task<WorkflowExecution?> GetByIdAsync(Guid executionId, CancellationToken cancellationToken = default);
    Task<WorkflowExecution?> GetByIdAndOwnerAsync(Guid executionId, Guid ownerUserId, CancellationToken cancellationToken = default);
    Task<WorkflowExecution?> GetByN8nExecutionIdAsync(string n8nExecutionId, CancellationToken cancellationToken = default);
    Task<WorkflowExecution?> GetByIdempotencyKeyAsync(string idempotencyKey, CancellationToken cancellationToken = default);
    Task<List<WorkflowExecution>> GetByWorkflowIdAsync(Guid workflowId, CancellationToken cancellationToken = default);
    Task<List<WorkflowExecution>> GetByOwnerAsync(Guid ownerUserId, int limit = 50, CancellationToken cancellationToken = default);
    Task<List<WorkflowExecution>> GetByStatusAsync(WorkflowExecutionStatus status, CancellationToken cancellationToken = default);
    Task<List<WorkflowExecution>> GetPendingApprovalsAsync(Guid ownerUserId, CancellationToken cancellationToken = default);
    Task AddAsync(WorkflowExecution execution, CancellationToken cancellationToken = default);
    Task UpdateAsync(WorkflowExecution execution, CancellationToken cancellationToken = default);
}
