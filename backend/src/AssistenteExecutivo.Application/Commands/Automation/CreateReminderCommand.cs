using MediatR;

namespace AssistenteExecutivo.Application.Commands.Automation;

public class CreateReminderCommand : IRequest<Guid>
{
    public Guid OwnerUserId { get; set; }
    public Guid ContactId { get; set; }
    public string Reason { get; set; } = string.Empty;
    public string? SuggestedMessage { get; set; }
    public DateTime ScheduledFor { get; set; }
}





