using MediatR;

namespace AssistenteExecutivo.Application.Queries.Automation;

public class GetReminderByIdQuery : IRequest<ReminderDto?>
{
    public Guid ReminderId { get; set; }
    public Guid OwnerUserId { get; set; }
}





