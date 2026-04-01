using AssistenteExecutivo.Domain.DomainEvents;
using AssistenteExecutivo.Domain.Entities;
using AssistenteExecutivo.Domain.Enums;
using AssistenteExecutivo.Domain.Exceptions;
using AssistenteExecutivo.Domain.Interfaces;
using AssistenteExecutivo.Domain.ValueObjects;
using FluentAssertions;

namespace AssistenteExecutivo.Domain.Tests.Entities;

public class CommissionAgreementTests
{
    private readonly IClock _clock = new TestClock();

    [Fact]
    public void Create_ShouldRaiseAgreementCreatedEvent()
    {
        var agreement = CreateAgreement();

        agreement.DomainEvents.Should().ContainSingle(e => e is AgreementCreated);
        agreement.Title.Should().Be("Projeto Alpha");
        agreement.Status.Should().Be(AgreementStatus.Draft);
    }

    [Fact]
    public void AddParty_WhenSplitExceedsHundred_ShouldThrow()
    {
        var agreement = CreateAgreement();
        agreement.AddParty(Guid.NewGuid(), null, null, "Parte A", "a@example.com", Percentage.Create(80), PartyRole.Agent, null, _clock);

        var act = () => agreement.AddParty(
            Guid.NewGuid(),
            null,
            null,
            "Parte B",
            "b@example.com",
            Percentage.Create(25),
            PartyRole.Agent,
            null,
            _clock);

        act.Should().Throw<DomainException>()
            .WithMessage("*SplitTotalNaoPodeExcederCem*");
    }

    [Fact]
    public void CompleteMilestone_ShouldEmitDomainEvent()
    {
        var agreement = CreateAgreement();
        agreement.ClearDomainEvents();
        var milestone = agreement.AddMilestone(Guid.NewGuid(), "Entrega fase 1", Money.Create(1000, "BRL"), DateTime.UtcNow.AddDays(7), _clock);
        agreement.ClearDomainEvents();

        agreement.CompleteMilestone(milestone.MilestoneId, "ok", null, _clock);

        agreement.DomainEvents.Should().ContainSingle(e => e is MilestoneCompleted);
        milestone.Status.Should().Be(MilestoneStatus.Completed);
        milestone.CompletionNotes.Should().Be("ok");
    }

    private CommissionAgreement CreateAgreement(decimal totalValue = 2000m)
    {
        return CommissionAgreement.Create(
            Guid.NewGuid(),
            Guid.NewGuid(),
            "Projeto Alpha",
            "Venda consultiva",
            Money.Create(totalValue, "BRL"),
            null,
            _clock);
    }

    private sealed class TestClock : IClock
    {
        public DateTime UtcNow { get; private set; } = DateTime.UtcNow;
    }
}
