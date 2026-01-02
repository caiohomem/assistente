using AssistenteExecutivo.Domain.Entities;
using AssistenteExecutivo.Domain.Enums;

namespace AssistenteExecutivo.Application.Interfaces;

public interface IReminderRepository
{
    Task<Reminder?> GetByIdAsync(Guid reminderId, Guid ownerUserId, CancellationToken cancellationToken = default);
    Task<List<Reminder>> GetByOwnerIdAsync(Guid ownerUserId, CancellationToken cancellationToken = default);
    Task<List<Reminder>> GetByContactIdAsync(Guid contactId, Guid ownerUserId, CancellationToken cancellationToken = default);
    Task<List<Reminder>> GetPendingByDateRangeAsync(DateTime startDate, DateTime endDate, CancellationToken cancellationToken = default);
    Task<List<Reminder>> GetByStatusAsync(ReminderStatus status, Guid ownerUserId, CancellationToken cancellationToken = default);
    Task AddAsync(Reminder reminder, CancellationToken cancellationToken = default);
    Task UpdateAsync(Reminder reminder, CancellationToken cancellationToken = default);
    Task DeleteAsync(Reminder reminder, CancellationToken cancellationToken = default);
}









