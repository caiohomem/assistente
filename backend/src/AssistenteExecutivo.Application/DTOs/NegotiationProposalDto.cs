using AssistenteExecutivo.Domain.Enums;

namespace AssistenteExecutivo.Application.DTOs;

public class NegotiationProposalDto
{
    public Guid ProposalId { get; set; }
    public Guid SessionId { get; set; }
    public Guid? PartyId { get; set; }
    public ProposalSource Source { get; set; }
    public ProposalStatus Status { get; set; }
    public string Content { get; set; } = string.Empty;
    public string? RejectionReason { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? RespondedAt { get; set; }
}
