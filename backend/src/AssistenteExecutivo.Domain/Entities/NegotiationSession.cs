using System;
using System.Linq;
using AssistenteExecutivo.Domain.DomainEvents;
using AssistenteExecutivo.Domain.Enums;
using AssistenteExecutivo.Domain.Exceptions;
using AssistenteExecutivo.Domain.Interfaces;

namespace AssistenteExecutivo.Domain.Entities;

public class NegotiationSession
{
    private const int MaxPendingProposalsPerParty = 5;
    private const int MaxPendingProposalsOverall = 25;
    private const int MaxAiSuggestions = 15;
    public static readonly TimeSpan AiSuggestionCooldownPeriod = TimeSpan.FromMinutes(5);

    private readonly List<NegotiationProposal> _proposals = new();
    private readonly List<IDomainEvent> _domainEvents = new();

    private NegotiationSession() { } // EF Core

    private NegotiationSession(
        Guid sessionId,
        Guid ownerUserId,
        string title,
        string? context,
        IClock clock)
    {
        if (sessionId == Guid.Empty)
            throw new DomainException("Domain:SessionIdObrigatorio");

        if (ownerUserId == Guid.Empty)
            throw new DomainException("Domain:OwnerUserIdObrigatorio");

        if (string.IsNullOrWhiteSpace(title))
            throw new DomainException("Domain:TituloObrigatorio");

        SessionId = sessionId;
        OwnerUserId = ownerUserId;
        Title = title.Trim();
        Context = string.IsNullOrWhiteSpace(context) ? null : context.Trim();
        Status = NegotiationStatus.Open;
        CreatedAt = clock.UtcNow;
        UpdatedAt = clock.UtcNow;

        _domainEvents.Add(new NegotiationSessionCreated(SessionId, OwnerUserId, Title, clock.UtcNow));
    }

    public Guid SessionId { get; private set; }
    public Guid OwnerUserId { get; private set; }
    public string Title { get; private set; } = string.Empty;
    public string? Context { get; private set; }
    public NegotiationStatus Status { get; private set; }
    public Guid? GeneratedAgreementId { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime UpdatedAt { get; private set; }
    public DateTime? LastAiSuggestionRequestedAt { get; private set; }
    public IReadOnlyCollection<NegotiationProposal> Proposals => _proposals.AsReadOnly();
    public IReadOnlyCollection<IDomainEvent> DomainEvents => _domainEvents.AsReadOnly();

    public static NegotiationSession Create(
        Guid sessionId,
        Guid ownerUserId,
        string title,
        string? context,
        IClock clock)
    {
        return new NegotiationSession(sessionId, ownerUserId, title, context, clock);
    }

    public NegotiationProposal SubmitProposal(
        Guid proposalId,
        Guid? partyId,
        ProposalSource source,
        string content,
        IClock clock)
    {
        if (Status != NegotiationStatus.Open)
            throw new DomainException("Domain:SessaoNaoPermiteNovasPropostas");

        EnsureProposalRules(partyId, source, clock);

        var proposal = NegotiationProposal.Create(proposalId, SessionId, partyId, source, content, clock);
        _proposals.Add(proposal);
        UpdatedAt = clock.UtcNow;
        if (source == ProposalSource.AI)
        {
            LastAiSuggestionRequestedAt = clock.UtcNow;
        }

        _domainEvents.Add(new ProposalSubmitted(SessionId, proposal.ProposalId, source, partyId, clock.UtcNow));
        return proposal;
    }

    public void AcceptProposal(Guid proposalId, Guid? partyId, IClock clock)
    {
        var proposal = GetProposal(proposalId);
        proposal.Accept(clock);
        UpdatedAt = clock.UtcNow;

        // Supersede outras propostas pendentes
        foreach (var other in _proposals.Where(p => p.ProposalId != proposalId && p.Status == ProposalStatus.Pending))
        {
            other.Supersede(clock);
        }

        Status = NegotiationStatus.Resolved;

        _domainEvents.Add(new ProposalAccepted(SessionId, proposalId, partyId, clock.UtcNow));
    }

    public void RejectProposal(Guid proposalId, Guid? partyId, string reason, IClock clock)
    {
        var proposal = GetProposal(proposalId);
        proposal.Reject(reason, clock);
        UpdatedAt = clock.UtcNow;

        _domainEvents.Add(new ProposalRejected(SessionId, proposalId, partyId, reason, clock.UtcNow));
    }

    public void CloseWithoutAgreement(IClock clock)
    {
        Status = NegotiationStatus.Closed;
        UpdatedAt = clock.UtcNow;
    }

    public void GenerateAgreement(Guid agreementId, IClock clock)
    {
        if (agreementId == Guid.Empty)
            throw new DomainException("Domain:AgreementIdObrigatorio");

        GeneratedAgreementId = agreementId;
        Status = NegotiationStatus.AgreementGenerated;
        UpdatedAt = clock.UtcNow;

        _domainEvents.Add(new AgreementGeneratedFromNegotiation(SessionId, agreementId, clock.UtcNow));
    }

    public void ClearDomainEvents() => _domainEvents.Clear();

    private NegotiationProposal GetProposal(Guid proposalId)
    {
        var proposal = _proposals.FirstOrDefault(p => p.ProposalId == proposalId);
        if (proposal == null)
            throw new DomainException("Domain:PropostaNaoEncontrada");

        return proposal;
    }

    public void RequestAiSuggestion(string? instructions, IClock clock)
    {
        if (Status != NegotiationStatus.Open)
            throw new DomainException("Domain:SessaoNaoPermiteSugestoes");

        EnsurePendingProposalLimits(null);
        EnsureAiSuggestionRules(clock, enforceRequestCooldown: true);

        LastAiSuggestionRequestedAt = clock.UtcNow;
        UpdatedAt = clock.UtcNow;

        _domainEvents.Add(new NegotiationAiSuggestionRequested(SessionId, OwnerUserId, instructions, clock.UtcNow));
    }

    private void EnsureProposalRules(Guid? partyId, ProposalSource source, IClock clock)
    {
        EnsurePendingProposalLimits(partyId);

        if (source == ProposalSource.AI)
        {
            EnsureAiSuggestionRules(clock, enforceRequestCooldown: false);
        }
    }

    private void EnsurePendingProposalLimits(Guid? partyId)
    {
        var pendingProposals = _proposals.Count(p => p.Status == ProposalStatus.Pending);
        if (pendingProposals >= MaxPendingProposalsOverall)
            throw new DomainException("Domain:LimitePropostasPendentesAtingido");

        if (partyId.HasValue)
        {
            var partyPending = _proposals.Count(p =>
                p.Status == ProposalStatus.Pending &&
                p.PartyId.HasValue &&
                p.PartyId.Value == partyId.Value);

            if (partyPending >= MaxPendingProposalsPerParty)
                throw new DomainException("Domain:ParteAtingiuLimitePropostasPendentes");
        }
    }

    private void EnsureAiSuggestionRules(IClock clock, bool enforceRequestCooldown)
    {
        if (string.IsNullOrWhiteSpace(Context))
            throw new DomainException("Domain:SessaoPrecisaDeContextoParaPropostaAI");

        var aiSuggestions = _proposals
            .Where(p => p.Source == ProposalSource.AI)
            .OrderByDescending(p => p.CreatedAt)
            .ToList();

        if (aiSuggestions.Count >= MaxAiSuggestions)
            throw new DomainException("Domain:LimiteSugestoesAIAtingido");

        var lastAiProposal = aiSuggestions.FirstOrDefault();
        if (lastAiProposal != null)
        {
            var elapsed = clock.UtcNow - lastAiProposal.CreatedAt;
            if (elapsed < AiSuggestionCooldownPeriod)
                throw new DomainException("Domain:CooldownIAEmAndamento");
        }

        if (enforceRequestCooldown && LastAiSuggestionRequestedAt.HasValue)
        {
            var elapsed = clock.UtcNow - LastAiSuggestionRequestedAt.Value;
            if (elapsed < AiSuggestionCooldownPeriod)
                throw new DomainException("Domain:CooldownIAEmAndamento");
        }
    }
}
