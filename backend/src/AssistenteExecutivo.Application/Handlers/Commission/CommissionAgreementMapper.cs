using System.Linq;
using AssistenteExecutivo.Application.DTOs;
using AssistenteExecutivo.Domain.Entities;

namespace AssistenteExecutivo.Application.Handlers.Commission;

internal static class CommissionAgreementMapper
{
    public static CommissionAgreementDto Map(CommissionAgreement agreement)
    {
        return new CommissionAgreementDto
        {
            AgreementId = agreement.AgreementId,
            OwnerUserId = agreement.OwnerUserId,
            Title = agreement.Title,
            Description = agreement.Description,
            Terms = agreement.Terms,
            TotalValue = agreement.TotalValue.Amount,
            Currency = agreement.TotalValue.Currency,
            Status = agreement.Status,
            EscrowAccountId = agreement.EscrowAccountId,
            CreatedAt = agreement.CreatedAt,
            UpdatedAt = agreement.UpdatedAt,
            ActivatedAt = agreement.ActivatedAt,
            CompletedAt = agreement.CompletedAt,
            CanceledAt = agreement.CanceledAt,
            Parties = agreement.Parties.Select(p => new AgreementPartyDto
            {
                PartyId = p.PartyId,
                ContactId = p.ContactId,
                CompanyId = p.CompanyId,
                PartyName = p.PartyName,
                Email = p.Email,
                SplitPercentage = p.SplitPercentage.Value,
                Role = p.Role,
                HasAccepted = p.HasAccepted,
                AcceptedAt = p.AcceptedAt
            }).ToList(),
            Milestones = agreement.Milestones.Select(m => new MilestoneDto
            {
                MilestoneId = m.MilestoneId,
                AgreementId = m.AgreementId,
                Description = m.Description,
                Value = m.Value.Amount,
                Currency = m.Value.Currency,
                DueDate = m.DueDate,
                Status = m.Status,
                CreatedAt = m.CreatedAt,
                CompletedAt = m.CompletedAt,
                CompletionNotes = m.CompletionNotes,
                ReleasedPayoutTransactionId = m.ReleasedPayoutTransactionId
            }).ToList()
        };
    }
}
