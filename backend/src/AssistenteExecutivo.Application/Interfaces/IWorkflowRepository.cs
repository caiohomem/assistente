using AssistenteExecutivo.Domain.Entities;
using AssistenteExecutivo.Domain.Enums;

namespace AssistenteExecutivo.Application.Interfaces;

public interface IWorkflowRepository
{
    Task<Workflow?> GetByIdAsync(Guid workflowId, CancellationToken cancellationToken = default);
    Task<Workflow?> GetByIdAndOwnerAsync(Guid workflowId, Guid ownerUserId, CancellationToken cancellationToken = default);
    Task<Workflow?> GetByN8nWorkflowIdAsync(string n8nWorkflowId, CancellationToken cancellationToken = default);
    Task<Workflow?> GetByIdempotencyKeyAsync(string idempotencyKey, CancellationToken cancellationToken = default);
    Task<List<Workflow>> GetByOwnerAsync(Guid ownerUserId, CancellationToken cancellationToken = default);
    Task<List<Workflow>> GetByOwnerAndStatusAsync(Guid ownerUserId, WorkflowStatus status, CancellationToken cancellationToken = default);
    Task<bool> ExistsByNameAndOwnerAsync(string name, Guid ownerUserId, CancellationToken cancellationToken = default);
    Task AddAsync(Workflow workflow, CancellationToken cancellationToken = default);
    Task UpdateAsync(Workflow workflow, CancellationToken cancellationToken = default);
    Task DeleteAsync(Workflow workflow, CancellationToken cancellationToken = default);
}
