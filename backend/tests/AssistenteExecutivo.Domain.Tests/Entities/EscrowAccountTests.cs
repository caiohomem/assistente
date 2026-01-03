using AssistenteExecutivo.Domain.DomainEvents;
using AssistenteExecutivo.Domain.Entities;
using AssistenteExecutivo.Domain.Enums;
using AssistenteExecutivo.Domain.Exceptions;
using AssistenteExecutivo.Domain.Interfaces;
using AssistenteExecutivo.Domain.ValueObjects;
using FluentAssertions;

namespace AssistenteExecutivo.Domain.Tests.Entities;

public class EscrowAccountTests
{
    private readonly IClock _clock = new TestClock();

    [Fact]
    public void RegisterDeposit_ShouldIncreaseBalanceAndRaiseEvent()
    {
        var account = CreateAccount();
        account.ClearDomainEvents();

        account.RegisterDeposit(
            Guid.NewGuid(),
            Money.Create(500, "BRL"),
            "Depósito inicial",
            EscrowTransactionStatus.Completed,
            "pi_123",
            null,
            _clock);

        account.Balance.Amount.Should().Be(500);
        account.DomainEvents.Should().ContainSingle(e => e is EscrowDepositReceived);
    }

    [Fact]
    public void RequestPayout_WhenBalanceInsufficient_ShouldThrow()
    {
        var account = CreateAccount();

        var act = () => account.RequestPayout(
            Guid.NewGuid(),
            null,
            Money.Create(100, "BRL"),
            "saque",
            PayoutApprovalType.Automatic,
            null,
            _clock);

        act.Should().Throw<DomainException>()
            .WithMessage("*SaldoEscrowInsuficiente*");
    }

    private EscrowAccount CreateAccount()
    {
        return EscrowAccount.Create(
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            "BRL",
            _clock);
    }

    private sealed class TestClock : IClock
    {
        public DateTime UtcNow { get; private set; } = DateTime.UtcNow;
    }
}
