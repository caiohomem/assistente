using AssistenteExecutivo.Domain.Notifications;

namespace AssistenteExecutivo.Application.Interfaces;

public interface IEmailTemplateRepository
{
    Task<EmailTemplate?> GetByTypeAsync(EmailTemplateType templateType, CancellationToken cancellationToken = default);
    Task<EmailTemplate?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<List<EmailTemplate>> GetAllActiveAsync(CancellationToken cancellationToken = default);
    Task<List<EmailTemplate>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<List<EmailTemplate>> GetByTypeFilterAsync(EmailTemplateType? templateType, bool? activeOnly, CancellationToken cancellationToken = default);
    Task AddAsync(EmailTemplate template, CancellationToken cancellationToken = default);
    Task UpdateAsync(EmailTemplate template, CancellationToken cancellationToken = default);
    Task DeleteAsync(EmailTemplate template, CancellationToken cancellationToken = default);
}

