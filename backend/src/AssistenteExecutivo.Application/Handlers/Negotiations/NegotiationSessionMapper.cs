using System.Linq;
using AssistenteExecutivo.Application.DTOs;
using AssistenteExecutivo.Domain.Entities;
using AssistenteExecutivo.Domain.Interfaces;

namespace AssistenteExecutivo.Application.Handlers.Negotiations;

internal static class NegotiationSessionMapper
{
    public static NegotiationSessionDto Map(NegotiationSession session, IClock clock)
    {
        var pendingCount = session.Proposals.Count(p => p.Status == Domain.Enums.ProposalStatus.Pending);
        var aiCount = session.Proposals.Count(p => p.Source == Domain.Enums.ProposalSource.AI);

        var nextAiAt = session.LastAiSuggestionRequestedAt.HasValue
            ? session.LastAiSuggestionRequestedAt.Value + NegotiationSession.AiSuggestionCooldownPeriod
            : (DateTime?)null;

        return new NegotiationSessionDto
        {
            SessionId = session.SessionId,
            OwnerUserId = session.OwnerUserId,
            Title = session.Title,
            Context = session.Context,
            Status = session.Status,
            GeneratedAgreementId = session.GeneratedAgreementId,
            CreatedAt = session.CreatedAt,
            UpdatedAt = session.UpdatedAt,
            LastAiSuggestionRequestedAt = session.LastAiSuggestionRequestedAt,
            NextAiSuggestionAvailableAt = nextAiAt,
            AiSuggestionCooldownActive = nextAiAt.HasValue && nextAiAt.Value > clock.UtcNow,
            PendingProposalCount = pendingCount,
            AiProposalCount = aiCount,
            Proposals = session.Proposals
                .OrderByDescending(p => p.CreatedAt)
                .Select(p => new NegotiationProposalDto
                {
                    ProposalId = p.ProposalId,
                    SessionId = p.SessionId,
                    PartyId = p.PartyId,
                    Source = p.Source,
                    Status = p.Status,
                    Content = p.Content,
                    RejectionReason = p.RejectionReason,
                    CreatedAt = p.CreatedAt,
                    RespondedAt = p.RespondedAt
                })
                .ToList()
        };
    }
}
