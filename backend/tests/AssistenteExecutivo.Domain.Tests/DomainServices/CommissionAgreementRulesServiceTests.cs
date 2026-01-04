using AssistenteExecutivo.Domain.DomainServices;
using AssistenteExecutivo.Domain.Entities;
using AssistenteExecutivo.Domain.Enums;
using AssistenteExecutivo.Domain.Exceptions;
using AssistenteExecutivo.Domain.Interfaces;
using AssistenteExecutivo.Domain.ValueObjects;
using FluentAssertions;

namespace AssistenteExecutivo.Domain.Tests.DomainServices;

public class CommissionAgreementRulesServiceTests
{
    private readonly IClock _clock = new TestClock();
    private readonly CommissionAgreementRulesService _service;

    public CommissionAgreementRulesServiceTests()
    {
        _service = new CommissionAgreementRulesService(_clock);
    }

    [Fact]
    public void EnsureCanActivate_WhenSplitNotHundred_ShouldThrow()
    {
        var agreement = CreateAgreement();
        var partyA = agreement.AddParty(Guid.NewGuid(), null, null, "Parte A", null, Percentage.Create(40), PartyRole.Agent, null, _clock);
        var partyB = agreement.AddParty(Guid.NewGuid(), null, null, "Parte B", null, Percentage.Create(40), PartyRole.Agent, null, _clock);
        agreement.AcceptAgreement(partyA.PartyId, _clock);
        agreement.AcceptAgreement(partyB.PartyId, _clock);
        agreement.AddMilestone(Guid.NewGuid(), "Entrega", Money.Create(1000, "BRL"), DateTime.UtcNow.AddDays(1), _clock);

        var act = () => _service.EnsureCanActivate(agreement);

        act.Should().Throw<DomainException>()
            .WithMessage("*SplitTotalDeveSerCemPorCento*");
    }

    [Fact]
    public void CalculateOutstandingValue_ShouldReturnRemaining()
    {
        var agreement = CreateAgreement();
        agreement.AddMilestone(Guid.NewGuid(), "Fase 1", Money.Create(600, "BRL"), DateTime.UtcNow, _clock);
        var milestone2 = agreement.AddMilestone(Guid.NewGuid(), "Fase 2", Money.Create(400, "BRL"), DateTime.UtcNow, _clock);
        agreement.CompleteMilestone(milestone2.MilestoneId, null, null, _clock);

        var outstanding = _service.CalculateOutstandingValue(agreement);

        outstanding.Amount.Should().Be(600);
    }

    private CommissionAgreement CreateAgreement()
    {
        return CommissionAgreement.Create(
            Guid.NewGuid(),
            Guid.NewGuid(),
            "Venda",
            null,
            Money.Create(1000, "BRL"),
            null,
            _clock);
    }

    private sealed class TestClock : IClock
    {
        public DateTime UtcNow { get; private set; } = DateTime.UtcNow;
    }
}
