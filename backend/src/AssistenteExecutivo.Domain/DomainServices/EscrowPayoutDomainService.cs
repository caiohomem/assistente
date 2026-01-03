using AssistenteExecutivo.Domain.Entities;
using AssistenteExecutivo.Domain.Enums;
using AssistenteExecutivo.Domain.Exceptions;
using AssistenteExecutivo.Domain.ValueObjects;

namespace AssistenteExecutivo.Domain.DomainServices;

/// <summary>
/// Encapsulates rules that cross the agreement, milestone and escrow aggregates when releasing payouts.
/// </summary>
public class EscrowPayoutDomainService
{
    public void EnsureMilestoneEligibleForPayout(
        CommissionAgreement agreement,
        Milestone milestone,
        Money requestedAmount)
    {
        if (agreement == null)
            throw new DomainException("Domain:AcordoObrigatorio");
        if (milestone == null)
            throw new DomainException("Domain:MilestoneObrigatorio");
        if (requestedAmount == null)
            throw new DomainException("Domain:ValorPayoutObrigatorio");

        if (milestone.AgreementId != agreement.AgreementId)
            throw new DomainException("Domain:MilestoneNaoPertenceAoAcordo");

        if (milestone.Status != MilestoneStatus.Completed)
            throw new DomainException("Domain:MilestonePrecisaEstarCompleto");

        if (requestedAmount > milestone.Value)
            throw new DomainException("Domain:ValorPayoutNaoPodeUltrapassarMilestone");
    }

    public void EnsureEscrowCoverage(EscrowAccount escrowAccount, Money requestedAmount)
    {
        if (escrowAccount == null)
            throw new DomainException("Domain:EscrowAccountObrigatoria");
        if (requestedAmount == null)
            throw new DomainException("Domain:ValorPayoutObrigatorio");

        if (escrowAccount.Currency != requestedAmount.Currency)
            throw new DomainException("Domain:MoedaDiferenteDaContaEscrow");

        if (escrowAccount.Balance < requestedAmount)
            throw new DomainException("Domain:SaldoEscrowInsuficiente");
    }

    public PayoutApprovalType DetermineApprovalPolicy(CommissionAgreement agreement, Money requestedAmount)
    {
        if (agreement == null)
            throw new DomainException("Domain:AcordoObrigatorio");
        if (requestedAmount == null)
            throw new DomainException("Domain:ValorPayoutObrigatorio");

        if (agreement.TotalValue.Amount <= 0)
            throw new DomainException("Domain:ValorTotalAcordoInvalido");

        var ratio = requestedAmount.Amount / agreement.TotalValue.Amount;

        if (ratio <= 0.1m)
            return PayoutApprovalType.Automatic;

        if (ratio >= 0.5m)
            return PayoutApprovalType.Disputed;

        return PayoutApprovalType.ApprovalRequired;
    }
}
