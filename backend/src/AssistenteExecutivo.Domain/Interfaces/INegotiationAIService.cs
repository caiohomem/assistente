using AssistenteExecutivo.Domain.Enums;

namespace AssistenteExecutivo.Domain.Interfaces;

public class NegotiationProposalSnapshot
{
    public Guid ProposalId { get; set; }
    public Guid? PartyId { get; set; }
    public ProposalSource Source { get; set; }
    public ProposalStatus Status { get; set; }
    public string Content { get; set; } = string.Empty;
}

public class NegotiationSuggestion
{
    public string Summary { get; set; } = string.Empty;
    public string SuggestedTermsJson { get; set; } = string.Empty;
}

public interface INegotiationAIService
{
    Task<NegotiationSuggestion> SuggestIntermediateTermsAsync(
        string negotiationContext,
        IReadOnlyCollection<NegotiationProposalSnapshot> proposals,
        CancellationToken cancellationToken = default);

    Task<string> GenerateAgreementDraftAsync(
        string negotiationContext,
        IReadOnlyCollection<NegotiationProposalSnapshot> proposals,
        CancellationToken cancellationToken = default);
}
