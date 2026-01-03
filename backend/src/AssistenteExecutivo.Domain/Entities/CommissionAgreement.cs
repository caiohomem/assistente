using AssistenteExecutivo.Domain.DomainEvents;
using AssistenteExecutivo.Domain.Enums;
using AssistenteExecutivo.Domain.Exceptions;
using AssistenteExecutivo.Domain.Interfaces;
using AssistenteExecutivo.Domain.ValueObjects;

namespace AssistenteExecutivo.Domain.Entities;

public class CommissionAgreement
{
    private readonly List<AgreementParty> _parties = new();
    private readonly List<Milestone> _milestones = new();
    private readonly List<IDomainEvent> _domainEvents = new();

    private CommissionAgreement() { } // EF Core

    private CommissionAgreement(
        Guid agreementId,
        Guid ownerUserId,
        string title,
        string? description,
        Money totalValue,
        string? terms,
        IClock clock)
    {
        if (agreementId == Guid.Empty)
            throw new DomainException("Domain:AgreementIdObrigatorio");

        if (ownerUserId == Guid.Empty)
            throw new DomainException("Domain:OwnerUserIdObrigatorio");

        if (string.IsNullOrWhiteSpace(title))
            throw new DomainException("Domain:TituloAcordoObrigatorio");

        if (totalValue == null)
            throw new DomainException("Domain:ValorTotalObrigatorio");

        AgreementId = agreementId;
        OwnerUserId = ownerUserId;
        Title = title.Trim();
        Description = string.IsNullOrWhiteSpace(description) ? null : description.Trim();
        TotalValue = totalValue;
        Terms = string.IsNullOrWhiteSpace(terms) ? null : terms.Trim();
        Status = AgreementStatus.Draft;
        CreatedAt = clock.UtcNow;
        UpdatedAt = clock.UtcNow;

        _domainEvents.Add(new AgreementCreated(
            AgreementId,
            OwnerUserId,
            Title,
            TotalValue.Amount,
            TotalValue.Currency,
            clock.UtcNow));
    }

    public Guid AgreementId { get; private set; }
    public Guid OwnerUserId { get; private set; }
    public string Title { get; private set; } = string.Empty;
    public string? Description { get; private set; }
    public string? Terms { get; private set; }
    public Money TotalValue { get; private set; } = null!;
    public AgreementStatus Status { get; private set; }
    public Guid? EscrowAccountId { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime UpdatedAt { get; private set; }
    public DateTime? ActivatedAt { get; private set; }
    public DateTime? CompletedAt { get; private set; }
    public DateTime? CanceledAt { get; private set; }

    public IReadOnlyCollection<AgreementParty> Parties => _parties.AsReadOnly();
    public IReadOnlyCollection<Milestone> Milestones => _milestones.AsReadOnly();
    public IReadOnlyCollection<IDomainEvent> DomainEvents => _domainEvents.AsReadOnly();

    public static CommissionAgreement Create(
        Guid agreementId,
        Guid ownerUserId,
        string title,
        string? description,
        Money totalValue,
        string? terms,
        IClock clock)
    {
        return new CommissionAgreement(agreementId, ownerUserId, title, description, totalValue, terms, clock);
    }

    public AgreementParty AddParty(
        Guid partyId,
        Guid? contactId,
        Guid? companyId,
        string partyName,
        string? email,
        Percentage splitPercentage,
        PartyRole role,
        IClock clock)
    {
        if (Status != AgreementStatus.Draft)
            throw new DomainException("Domain:ApenasDraftPermitePartes");

        if (_parties.Any(p => p.PartyId == partyId))
            throw new DomainException("Domain:ParteJaExiste");

        EnsureSplitWithinLimit(splitPercentage);

        var party = AgreementParty.Create(
            partyId,
            contactId,
            companyId,
            partyName,
            email,
            splitPercentage,
            role,
            clock);

        _parties.Add(party);
        UpdatedAt = clock.UtcNow;

        _domainEvents.Add(new PartyAddedToAgreement(
            AgreementId,
            party.PartyId,
            party.PartyName,
            party.SplitPercentage.Value,
            party.Role,
            clock.UtcNow));

        return party;
    }

    public void AcceptAgreement(Guid partyId, IClock clock)
    {
        var party = GetParty(partyId);
        party.Accept(clock);
        UpdatedAt = clock.UtcNow;

        _domainEvents.Add(new PartyAcceptedAgreement(AgreementId, partyId, clock.UtcNow));
    }

    public Milestone AddMilestone(
        Guid milestoneId,
        string description,
        Money value,
        DateTime dueDate,
        IClock clock)
    {
        if (Status != AgreementStatus.Draft)
            throw new DomainException("Domain:ApenasDraftPermiteMilestones");

        if (_milestones.Any(m => m.MilestoneId == milestoneId))
            throw new DomainException("Domain:MilestoneJaExiste");

        EnsureMilestonesWithinTotal(value);

        var milestone = Milestone.Create(milestoneId, AgreementId, description, value, dueDate, clock);
        _milestones.Add(milestone);
        UpdatedAt = clock.UtcNow;

        _domainEvents.Add(new MilestoneCreated(
            AgreementId,
            milestone.MilestoneId,
            milestone.Description,
            milestone.DueDate,
            milestone.Value.Amount,
            milestone.Value.Currency,
            clock.UtcNow));

        return milestone;
    }

    public void CompleteMilestone(Guid milestoneId, string? notes, Guid? releasedTransactionId, IClock clock)
    {
        var milestone = GetMilestone(milestoneId);
        milestone.Complete(notes, releasedTransactionId, clock);
        UpdatedAt = clock.UtcNow;

        _domainEvents.Add(new MilestoneCompleted(AgreementId, milestoneId, notes, clock.UtcNow));
    }

    public void MarkMilestoneOverdue(Guid milestoneId, IClock clock)
    {
        var milestone = GetMilestone(milestoneId);
        milestone.MarkOverdue();
        UpdatedAt = clock.UtcNow;

        _domainEvents.Add(new MilestoneOverdue(AgreementId, milestoneId, milestone.DueDate, clock.UtcNow));
    }

    public void Activate(IClock clock)
    {
        if (Status != AgreementStatus.Draft)
            throw new DomainException("Domain:AcordoNaoEstaEmDraft");

        if (!_parties.Any())
            throw new DomainException("Domain:AcordoPrecisaDePartes");

        if (!_milestones.Any())
            throw new DomainException("Domain:AcordoPrecisaDeMilestones");

        if (_parties.Any(p => !p.HasAccepted))
            throw new DomainException("Domain:TodasPartesDevemAceitar");

        Status = AgreementStatus.Active;
        ActivatedAt = clock.UtcNow;
        UpdatedAt = clock.UtcNow;

        _domainEvents.Add(new AgreementActivated(AgreementId, clock.UtcNow));
    }

    public void Complete(IClock clock)
    {
        if (Status != AgreementStatus.Active)
            throw new DomainException("Domain:AcordoNaoEstaAtivo");

        if (_milestones.Any(m => m.Status != MilestoneStatus.Completed))
            throw new DomainException("Domain:TodosMilestonesDevemSerCompletos");

        Status = AgreementStatus.Completed;
        CompletedAt = clock.UtcNow;
        UpdatedAt = clock.UtcNow;

        _domainEvents.Add(new AgreementCompleted(AgreementId, clock.UtcNow));
    }

    public void Dispute(string reason, IClock clock)
    {
        if (Status == AgreementStatus.Canceled)
            throw new DomainException("Domain:AcordoCancelado");

        Status = AgreementStatus.Disputed;
        UpdatedAt = clock.UtcNow;

        _domainEvents.Add(new AgreementDisputed(AgreementId, reason, clock.UtcNow));
    }

    public void Cancel(string reason, IClock clock)
    {
        if (Status == AgreementStatus.Completed)
            throw new DomainException("Domain:AcordoJaConcluido");

        Status = AgreementStatus.Canceled;
        CanceledAt = clock.UtcNow;
        UpdatedAt = clock.UtcNow;

        _domainEvents.Add(new AgreementCanceled(AgreementId, reason, clock.UtcNow));
    }

    public void AttachEscrowAccount(Guid escrowAccountId)
    {
        if (escrowAccountId == Guid.Empty)
            throw new DomainException("Domain:EscrowAccountIdObrigatorio");

        EscrowAccountId = escrowAccountId;
    }

    public void UpdateDetails(string? title, string? description, string? terms)
    {
        if (!string.IsNullOrWhiteSpace(title))
            Title = title.Trim();

        if (description != null)
            Description = string.IsNullOrWhiteSpace(description) ? null : description.Trim();

        if (terms != null)
            Terms = string.IsNullOrWhiteSpace(terms) ? null : terms.Trim();

        UpdatedAt = DateTime.UtcNow;
    }

    public void ClearDomainEvents() => _domainEvents.Clear();

    private AgreementParty GetParty(Guid partyId)
    {
        var party = _parties.FirstOrDefault(p => p.PartyId == partyId);
        if (party == null)
            throw new DomainException("Domain:ParteNaoEncontrada");

        return party;
    }

    private Milestone GetMilestone(Guid milestoneId)
    {
        var milestone = _milestones.FirstOrDefault(m => m.MilestoneId == milestoneId);
        if (milestone == null)
            throw new DomainException("Domain:MilestoneNaoEncontrado");

        return milestone;
    }

    private void EnsureSplitWithinLimit(Percentage newSplit)
    {
        var total = _parties.Sum(p => p.SplitPercentage.Value) + newSplit.Value;
        if (total > 100m)
            throw new DomainException("Domain:SplitTotalNaoPodeExcederCem");
    }

    private void EnsureMilestonesWithinTotal(Money newValue)
    {
        var total = _milestones.Sum(m => m.Value.Amount) + newValue.Amount;
        if (total > TotalValue.Amount)
            throw new DomainException("Domain:SomaMilestonesMaiorQueTotal");
    }
}
