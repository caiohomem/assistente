using AssistenteExecutivo.Domain.Entities;
using AssistenteExecutivo.Domain.Enums;

namespace AssistenteExecutivo.Application.Interfaces;

public interface ITemplateRepository
{
    Task<Template?> GetByIdAsync(Guid templateId, Guid ownerUserId, CancellationToken cancellationToken = default);
    Task<List<Template>> GetByOwnerIdAsync(Guid ownerUserId, CancellationToken cancellationToken = default);
    Task<List<Template>> GetByTypeAsync(TemplateType type, Guid ownerUserId, CancellationToken cancellationToken = default);
    Task<List<Template>> GetActiveByOwnerIdAsync(Guid ownerUserId, CancellationToken cancellationToken = default);
    Task<List<Template>> GetActiveByTypeAsync(TemplateType type, Guid ownerUserId, CancellationToken cancellationToken = default);
    Task AddAsync(Template template, CancellationToken cancellationToken = default);
    Task UpdateAsync(Template template, CancellationToken cancellationToken = default);
    Task DeleteAsync(Template template, CancellationToken cancellationToken = default);
}

