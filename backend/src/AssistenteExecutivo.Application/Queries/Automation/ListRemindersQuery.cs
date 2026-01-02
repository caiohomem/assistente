using AssistenteExecutivo.Domain.Enums;
using MediatR;

namespace AssistenteExecutivo.Application.Queries.Automation;

public class ListRemindersQuery : IRequest<ListRemindersResultDto>
{
    public Guid OwnerUserId { get; set; }
    public Guid? ContactId { get; set; }
    public ReminderStatus? Status { get; set; }
    public DateTime? StartDate { get; set; }
    public DateTime? EndDate { get; set; }
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 20;
}

public class ListRemindersResultDto
{
    public List<ReminderDto> Reminders { get; set; } = new();
    public int Total { get; set; }
    public int Page { get; set; }
    public int PageSize { get; set; }
    public int TotalPages { get; set; }
}

public class ReminderDto
{
    public Guid ReminderId { get; set; }
    public Guid OwnerUserId { get; set; }
    public Guid ContactId { get; set; }
    public string Reason { get; set; } = string.Empty;
    public string? SuggestedMessage { get; set; }
    public DateTime ScheduledFor { get; set; }
    public ReminderStatus Status { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}









