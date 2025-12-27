using AssistenteExecutivo.Application.Interfaces;
using AssistenteExecutivo.Application.Queries.Automation;
using MediatR;

namespace AssistenteExecutivo.Application.Handlers.Automation;

public class GetReminderByIdQueryHandler : IRequestHandler<GetReminderByIdQuery, ReminderDto?>
{
    private readonly IReminderRepository _reminderRepository;

    public GetReminderByIdQueryHandler(IReminderRepository reminderRepository)
    {
        _reminderRepository = reminderRepository;
    }

    public async Task<ReminderDto?> Handle(GetReminderByIdQuery request, CancellationToken cancellationToken)
    {
        var reminder = await _reminderRepository.GetByIdAsync(request.ReminderId, request.OwnerUserId, cancellationToken);
        if (reminder == null)
            return null;

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

