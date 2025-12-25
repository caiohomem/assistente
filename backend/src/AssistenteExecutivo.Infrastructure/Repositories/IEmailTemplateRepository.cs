using AssistenteExecutivo.Domain.Notifications;

namespace AssistenteExecutivo.Infrastructure.Repositories;

public interface IEmailTemplateRepository
{
    Task<EmailTemplate?> GetByTypeAsync(EmailTemplateType templateType, CancellationToken cancellationToken = default);
    Task<EmailTemplate?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<List<EmailTemplate>> GetAllActiveAsync(CancellationToken cancellationToken = default);
    Task AddAsync(EmailTemplate template, CancellationToken cancellationToken = default);
    Task UpdateAsync(EmailTemplate template, CancellationToken cancellationToken = default);
}

