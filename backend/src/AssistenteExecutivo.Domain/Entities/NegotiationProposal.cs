using AssistenteExecutivo.Domain.Enums;
using AssistenteExecutivo.Domain.Exceptions;
using AssistenteExecutivo.Domain.Interfaces;

namespace AssistenteExecutivo.Domain.Entities;

public class NegotiationProposal
{
    private NegotiationProposal() { } // EF Core

    private NegotiationProposal(
        Guid proposalId,
        Guid sessionId,
        Guid? partyId,
        ProposalSource source,
        string content,
        IClock clock)
    {
        if (proposalId == Guid.Empty)
            throw new DomainException("Domain:ProposalIdObrigatorio");

        if (sessionId == Guid.Empty)
            throw new DomainException("Domain:SessionIdObrigatorio");

        if (string.IsNullOrWhiteSpace(content))
            throw new DomainException("Domain:ConteudoPropostaObrigatorio");

        ProposalId = proposalId;
        SessionId = sessionId;
        PartyId = partyId;
        Source = source;
        Content = content;
        Status = ProposalStatus.Pending;
        CreatedAt = clock.UtcNow;
    }

    public Guid ProposalId { get; private set; }
    public Guid SessionId { get; private set; }
    public Guid? PartyId { get; private set; }
    public ProposalSource Source { get; private set; }
    public string Content { get; private set; } = string.Empty;
    public ProposalStatus Status { get; private set; }
    public string? RejectionReason { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime? RespondedAt { get; private set; }

    public static NegotiationProposal Create(
        Guid proposalId,
        Guid sessionId,
        Guid? partyId,
        ProposalSource source,
        string content,
        IClock clock)
    {
        return new NegotiationProposal(proposalId, sessionId, partyId, source, content, clock);
    }

    internal void Accept(IClock clock)
    {
        if (Status == ProposalStatus.Accepted)
            return;

        Status = ProposalStatus.Accepted;
        RejectionReason = null;
        RespondedAt = clock.UtcNow;
    }

    internal void Reject(string reason, IClock clock)
    {
        if (Status == ProposalStatus.Rejected)
            return;

        Status = ProposalStatus.Rejected;
        RejectionReason = string.IsNullOrWhiteSpace(reason) ? "Sem motivo" : reason.Trim();
        RespondedAt = clock.UtcNow;
    }

    internal void Supersede(IClock clock)
    {
        Status = ProposalStatus.Superseded;
        RespondedAt = clock.UtcNow;
    }
}
