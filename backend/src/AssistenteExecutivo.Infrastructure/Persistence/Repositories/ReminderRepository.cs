using AssistenteExecutivo.Application.Interfaces;
using AssistenteExecutivo.Domain.Entities;
using AssistenteExecutivo.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace AssistenteExecutivo.Infrastructure.Persistence.Repositories;

public class ReminderRepository : IReminderRepository
{
    private readonly ApplicationDbContext _context;

    public ReminderRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<Reminder?> GetByIdAsync(Guid reminderId, Guid ownerUserId, CancellationToken cancellationToken = default)
    {
        return await _context.Reminders
            .FirstOrDefaultAsync(r => r.ReminderId == reminderId && r.OwnerUserId == ownerUserId, cancellationToken);
    }

    public async Task<List<Reminder>> GetByOwnerIdAsync(Guid ownerUserId, CancellationToken cancellationToken = default)
    {
        return await _context.Reminders
            .Where(r => r.OwnerUserId == ownerUserId)
            .ToListAsync(cancellationToken);
    }

    public async Task<List<Reminder>> GetByContactIdAsync(Guid contactId, Guid ownerUserId, CancellationToken cancellationToken = default)
    {
        return await _context.Reminders
            .Where(r => r.ContactId == contactId && r.OwnerUserId == ownerUserId)
            .ToListAsync(cancellationToken);
    }

    public async Task<List<Reminder>> GetPendingByDateRangeAsync(DateTime startDate, DateTime endDate, CancellationToken cancellationToken = default)
    {
        return await _context.Reminders
            .Where(r => r.Status == ReminderStatus.Pending && 
                       r.ScheduledFor >= startDate && 
                       r.ScheduledFor <= endDate)
            .ToListAsync(cancellationToken);
    }

    public async Task<List<Reminder>> GetByStatusAsync(ReminderStatus status, Guid ownerUserId, CancellationToken cancellationToken = default)
    {
        return await _context.Reminders
            .Where(r => r.Status == status && r.OwnerUserId == ownerUserId)
            .ToListAsync(cancellationToken);
    }

    public async Task AddAsync(Reminder reminder, CancellationToken cancellationToken = default)
    {
        await _context.Reminders.AddAsync(reminder, cancellationToken);
    }

    public Task UpdateAsync(Reminder reminder, CancellationToken cancellationToken = default)
    {
        _context.Reminders.Update(reminder);
        return Task.CompletedTask;
    }

    public Task DeleteAsync(Reminder reminder, CancellationToken cancellationToken = default)
    {
        _context.Reminders.Remove(reminder);
        return Task.CompletedTask;
    }
}

