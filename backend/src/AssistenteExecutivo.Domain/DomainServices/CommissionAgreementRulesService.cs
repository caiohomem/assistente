using AssistenteExecutivo.Domain.Entities;
using AssistenteExecutivo.Domain.Enums;
using AssistenteExecutivo.Domain.Exceptions;
using AssistenteExecutivo.Domain.Interfaces;
using AssistenteExecutivo.Domain.ValueObjects;

namespace AssistenteExecutivo.Domain.DomainServices;

/// <summary>
/// Aggregates cross-entity rules that are relevant to the lifecycle of commission agreements.
/// Application layer should rely on this service before issuing commands to avoid violating invariants.
/// </summary>
public class CommissionAgreementRulesService
{
    private readonly IClock _clock;

    public CommissionAgreementRulesService(IClock clock)
    {
        _clock = clock ?? throw new DomainException("Domain:ClockObrigatorio");
    }

    public void EnsureCanActivate(CommissionAgreement agreement)
    {
        if (agreement == null)
            throw new DomainException("Domain:AcordoObrigatorio");

        if (agreement.Status != AgreementStatus.Draft)
            throw new DomainException("Domain:AcordoPrecisaEstarEmDraft");

        if (!agreement.Parties.Any())
            throw new DomainException("Domain:AcordoPrecisaDePartes");

        if (!agreement.Milestones.Any())
            throw new DomainException("Domain:AcordoPrecisaDeMilestones");

        var totalSplit = agreement.Parties.Sum(p => p.SplitPercentage.Value);
        if (totalSplit != 100m)
            throw new DomainException("Domain:SplitTotalDeveSerCemPorCento");

        var milestonesTotal = agreement.Milestones.Sum(m => m.Value.Amount);
        if (milestonesTotal != agreement.TotalValue.Amount)
            throw new DomainException("Domain:MilestonesDevemFecharValorTotal");
    }

    public void EnsureCanComplete(CommissionAgreement agreement)
    {
        if (agreement == null)
            throw new DomainException("Domain:AcordoObrigatorio");

        if (agreement.Status != AgreementStatus.Active)
            throw new DomainException("Domain:AcordoPrecisaEstarAtivo");

        if (agreement.Milestones.Any(m => m.Status != MilestoneStatus.Completed))
            throw new DomainException("Domain:TodosMilestonesDevemEstarCompletos");
    }

    public Money CalculateOutstandingValue(CommissionAgreement agreement)
    {
        if (agreement == null)
            throw new DomainException("Domain:AcordoObrigatorio");

        var released = agreement
            .Milestones
            .Where(m => m.Status == MilestoneStatus.Completed)
            .Sum(m => m.Value.Amount);

        var remaining = agreement.TotalValue.Amount - released;
        if (remaining < 0) remaining = 0;
        return Money.Create(remaining, agreement.TotalValue.Currency);
    }

    public IReadOnlyCollection<Milestone> GetOverdueMilestones(CommissionAgreement agreement)
    {
        if (agreement == null)
            throw new DomainException("Domain:AcordoObrigatorio");

        var now = _clock.UtcNow;
        return agreement
            .Milestones
            .Where(m => m.Status == MilestoneStatus.Pending && m.DueDate < now)
            .ToList();
    }
}
