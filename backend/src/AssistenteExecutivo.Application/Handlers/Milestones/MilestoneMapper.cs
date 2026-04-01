using AssistenteExecutivo.Application.DTOs;
using AssistenteExecutivo.Domain.Entities;

namespace AssistenteExecutivo.Application.Handlers.Milestones;

internal static class MilestoneMapper
{
    public static MilestoneDto Map(Milestone milestone)
    {
        return new MilestoneDto
        {
            MilestoneId = milestone.MilestoneId,
            AgreementId = milestone.AgreementId,
            Description = milestone.Description,
            Value = milestone.Value.Amount,
            Currency = milestone.Value.Currency,
            DueDate = milestone.DueDate,
            Status = milestone.Status,
            CreatedAt = milestone.CreatedAt,
            CompletedAt = milestone.CompletedAt,
            CompletionNotes = milestone.CompletionNotes,
            ReleasedPayoutTransactionId = milestone.ReleasedPayoutTransactionId
        };
    }
}
