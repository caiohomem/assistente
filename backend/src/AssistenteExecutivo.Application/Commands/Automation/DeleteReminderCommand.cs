using MediatR;

namespace AssistenteExecutivo.Application.Commands.Automation;

public class DeleteReminderCommand : IRequest
{
    public Guid ReminderId { get; set; }
    public Guid OwnerUserId { get; set; }
}







