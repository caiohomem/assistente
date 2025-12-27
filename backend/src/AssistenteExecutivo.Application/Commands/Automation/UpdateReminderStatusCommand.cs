using AssistenteExecutivo.Domain.Enums;
using MediatR;

namespace AssistenteExecutivo.Application.Commands.Automation;

public class UpdateReminderStatusCommand : IRequest
{
    public Guid ReminderId { get; set; }
    public Guid OwnerUserId { get; set; }
    public ReminderStatus NewStatus { get; set; }
    public DateTime? NewScheduledFor { get; set; } // Para Snoozed
}

