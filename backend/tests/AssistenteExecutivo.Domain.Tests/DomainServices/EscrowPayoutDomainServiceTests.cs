using AssistenteExecutivo.Domain.DomainServices;
using AssistenteExecutivo.Domain.Entities;
using AssistenteExecutivo.Domain.Enums;
using AssistenteExecutivo.Domain.Exceptions;
using AssistenteExecutivo.Domain.Interfaces;
using AssistenteExecutivo.Domain.ValueObjects;
using FluentAssertions;

namespace AssistenteExecutivo.Domain.Tests.DomainServices;

public class EscrowPayoutDomainServiceTests
{
    private readonly EscrowPayoutDomainService _service = new();
    private readonly IClock _clock = new TestClock();

    [Fact]
    public void EnsureMilestoneEligibleForPayout_WhenRequestGreaterThanMilestone_ShouldThrow()
    {
        var agreement = CreateAgreementWithMilestone(out var milestone);

        var act = () => _service.EnsureMilestoneEligibleForPayout(
            agreement,
            milestone,
            Money.Create(1500, "BRL"));

        act.Should().Throw<DomainException>()
            .WithMessage("*ValorPayoutNaoPodeUltrapassarMilestone*");
    }

    [Fact]
    public void DetermineApprovalPolicy_ShouldReturnDisputedForLargeValue()
    {
        var agreement = CreateAgreementWithMilestone(out _);
        var approval = _service.DetermineApprovalPolicy(agreement, Money.Create(600, "BRL"));
        approval.Should().Be(PayoutApprovalType.Disputed);
    }

    [Fact]
    public void EnsureEscrowCoverage_WhenCurrencyDiffers_ShouldThrow()
    {
        var escrow = EscrowAccount.Create(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), "USD", _clock);

        var act = () => _service.EnsureEscrowCoverage(escrow, Money.Create(10, "BRL"));

        act.Should().Throw<DomainException>()
            .WithMessage("*MoedaDiferenteDaContaEscrow*");
    }

    private CommissionAgreement CreateAgreementWithMilestone(out Milestone milestone)
    {
        var agreement = CommissionAgreement.Create(
            Guid.NewGuid(),
            Guid.NewGuid(),
            "Comissão",
            null,
            Money.Create(1000, "BRL"),
            null,
            _clock);

        milestone = agreement.AddMilestone(Guid.NewGuid(), "Entrega", Money.Create(1000, "BRL"), DateTime.UtcNow.AddDays(1), _clock);
        return agreement;
    }

    private sealed class TestClock : IClock
    {
        public DateTime UtcNow { get; private set; } = DateTime.UtcNow;
    }
}
