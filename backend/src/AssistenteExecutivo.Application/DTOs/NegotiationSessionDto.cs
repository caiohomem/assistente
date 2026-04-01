using AssistenteExecutivo.Domain.Enums;

namespace AssistenteExecutivo.Application.DTOs;

public class NegotiationSessionDto
{
    public Guid SessionId { get; set; }
    public Guid OwnerUserId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? Context { get; set; }
    public NegotiationStatus Status { get; set; }
    public Guid? GeneratedAgreementId { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public DateTime? LastAiSuggestionRequestedAt { get; set; }
    public DateTime? NextAiSuggestionAvailableAt { get; set; }
    public bool AiSuggestionCooldownActive { get; set; }
    public int PendingProposalCount { get; set; }
    public int AiProposalCount { get; set; }
    public List<NegotiationProposalDto> Proposals { get; set; } = new();
}
