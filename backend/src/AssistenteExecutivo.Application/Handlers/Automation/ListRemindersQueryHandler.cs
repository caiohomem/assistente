using AssistenteExecutivo.Application.Interfaces;
using AssistenteExecutivo.Application.Queries.Automation;
using MediatR;

namespace AssistenteExecutivo.Application.Handlers.Automation;

public class ListRemindersQueryHandler : IRequestHandler<ListRemindersQuery, ListRemindersResultDto>
{
    private readonly IReminderRepository _reminderRepository;

    public ListRemindersQueryHandler(IReminderRepository reminderRepository)
    {
        _reminderRepository = reminderRepository;
    }

    public async Task<ListRemindersResultDto> Handle(ListRemindersQuery request, CancellationToken cancellationToken)
    {
        var page = Math.Max(1, request.Page);
        var pageSize = Math.Max(1, Math.Min(100, request.PageSize));

        List<Domain.Entities.Reminder> reminders;

        if (request.ContactId.HasValue)
        {
            reminders = await _reminderRepository.GetByContactIdAsync(request.ContactId.Value, request.OwnerUserId, cancellationToken);
        }
        else if (request.Status.HasValue)
        {
            reminders = await _reminderRepository.GetByStatusAsync(request.Status.Value, request.OwnerUserId, cancellationToken);
        }
        else
        {
            reminders = await _reminderRepository.GetByOwnerIdAsync(request.OwnerUserId, cancellationToken);
        }

        // Filtrar por data se fornecido
        if (request.StartDate.HasValue || request.EndDate.HasValue)
        {
            reminders = reminders.Where(r =>
                (!request.StartDate.HasValue || r.ScheduledFor >= request.StartDate.Value) &&
                (!request.EndDate.HasValue || r.ScheduledFor <= request.EndDate.Value))
                .ToList();
        }

        var total = reminders.Count;
        var totalPages = (int)Math.Ceiling(total / (double)pageSize);
        var skip = (page - 1) * pageSize;

        var paginatedReminders = reminders
            .OrderBy(r => r.ScheduledFor)
            .Skip(skip)
            .Take(pageSize)
            .ToList();

        return new ListRemindersResultDto
        {
            Reminders = paginatedReminders.Select(MapToDto).ToList(),
            Total = total,
            Page = page,
            PageSize = pageSize,
            TotalPages = totalPages
        };
    }

    private static ReminderDto MapToDto(Domain.Entities.Reminder reminder)
    {
        return new ReminderDto
        {
            ReminderId = reminder.ReminderId,
            OwnerUserId = reminder.OwnerUserId,
            ContactId = reminder.ContactId,
            Reason = reminder.Reason,
            SuggestedMessage = reminder.SuggestedMessage,
            ScheduledFor = reminder.ScheduledFor,
            Status = reminder.Status,
            CreatedAt = reminder.CreatedAt,
            UpdatedAt = reminder.UpdatedAt
        };
    }
}





