using AssistenteExecutivo.Domain.DomainEvents;
using AssistenteExecutivo.Domain.Entities;
using AssistenteExecutivo.Domain.Enums;
using AssistenteExecutivo.Domain.Exceptions;
using AssistenteExecutivo.Domain.Interfaces;
using FluentAssertions;

namespace AssistenteExecutivo.Domain.Tests.Entities;

public class NegotiationSessionTests
{
    private readonly IClock _clock = new TestClock();

    [Fact]
    public void SubmitProposal_FromAi_ShouldRaiseEvent()
    {
        var session = CreateSession();
        session.ClearDomainEvents();

        session.SubmitProposal(
            Guid.NewGuid(),
            null,
            ProposalSource.AI,
            "{ \"proposal\": 1 }",
            _clock);

        session.DomainEvents.Should().ContainSingle(e => e is ProposalSubmitted);
        session.Proposals.Should().HaveCount(1);
    }

    [Fact]
    public void AcceptProposal_ShouldSetStatusResolvedAndRaiseEvent()
    {
        var session = CreateSession();
        session.ClearDomainEvents();
        var proposal = session.SubmitProposal(Guid.NewGuid(), null, ProposalSource.Party, "offer", _clock);
        session.ClearDomainEvents();

        session.AcceptProposal(proposal.ProposalId, null, _clock);

        session.Status.Should().Be(NegotiationStatus.Resolved);
        session.DomainEvents.Should().ContainSingle(e => e is ProposalAccepted);
    }

    [Fact]
    public void RequestAiSuggestion_WhenContextMissing_ShouldThrow()
    {
        var session = CreateSession(context: null);

        var act = () => session.RequestAiSuggestion(null, _clock);

        act.Should().Throw<DomainException>()
            .WithMessage("*SessaoPrecisaDeContextoParaPropostaAI*");
    }

    private NegotiationSession CreateSession(string? context = "contrato x")
    {
        return NegotiationSession.Create(
            Guid.NewGuid(),
            Guid.NewGuid(),
            "Sessão teste",
            context,
            _clock);
    }

    private sealed class TestClock : IClock
    {
        public DateTime UtcNow { get; private set; } = DateTime.UtcNow;
    }
}
