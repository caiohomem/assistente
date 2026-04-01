using AssistenteExecutivo.Application.Commands.Milestones;
using AssistenteExecutivo.Application.Handlers.Milestones;
using AssistenteExecutivo.Application.Interfaces;
using AssistenteExecutivo.Application.Tests.Helpers;
using AssistenteExecutivo.Domain.DomainServices;
using AssistenteExecutivo.Domain.Entities;
using AssistenteExecutivo.Domain.Enums;
using AssistenteExecutivo.Domain.Interfaces;
using AssistenteExecutivo.Domain.ValueObjects;
using FluentAssertions;
using MediatR;
using Moq;

namespace AssistenteExecutivo.Application.Tests.Handlers.Milestones;

public class TriggerMilestonePayoutCommandHandlerTests
{
    private readonly Mock<ICommissionAgreementRepository> _agreementRepository = new();
    private readonly Mock<IEscrowAccountRepository> _escrowRepository = new();
    private readonly Mock<IUnitOfWork> _unitOfWork = new();
    private readonly Mock<IPublisher> _publisher = new();
    private readonly TestClock _clock = new(DateTime.UtcNow);
    private readonly EscrowPayoutDomainService _domainService = new();

    private readonly TriggerMilestonePayoutCommandHandler _handler;

    public TriggerMilestonePayoutCommandHandlerTests()
    {
        _handler = new TriggerMilestonePayoutCommandHandler(
            _agreementRepository.Object,
            _escrowRepository.Object,
            _domainService,
            _unitOfWork.Object,
            _clock,
            _publisher.Object);
    }

    [Fact]
    public async Task Handle_ShouldRequestPayoutAndLinkTransactionToMilestone()
    {
        var agreement = BuildAgreement(out var milestone, out var escrowAccount);

        _agreementRepository.Setup(r => r.GetByIdAsync(agreement.AgreementId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(agreement);
        _escrowRepository.Setup(r => r.GetByIdAsync(escrowAccount.EscrowAccountId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(escrowAccount);
        _unitOfWork.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);

        var command = new TriggerMilestonePayoutCommand
        {
            AgreementId = agreement.AgreementId,
            MilestoneId = milestone.MilestoneId,
            RequestedBy = agreement.OwnerUserId
        };

        var transactionId = await _handler.Handle(command, CancellationToken.None);

        escrowAccount.Transactions.Should().Contain(t => t.TransactionId == transactionId && t.Type == EscrowTransactionType.Payout);
        milestone.ReleasedPayoutTransactionId.Should().Be(transactionId);
        _unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    private CommissionAgreement BuildAgreement(out Milestone milestone, out EscrowAccount escrowAccount)
    {
        var agreement = CommissionAgreement.Create(
            Guid.NewGuid(),
            Guid.NewGuid(),
            "Projeto",
            null,
            Money.Create(1000, "BRL"),
            null,
            _clock);

        agreement.AddParty(Guid.NewGuid(), null, null, "Parte A", null, Percentage.Create(60), PartyRole.Agent, _clock);
        agreement.AddParty(Guid.NewGuid(), null, null, "Parte B", null, Percentage.Create(40), PartyRole.Agent, _clock);
        milestone = agreement.AddMilestone(Guid.NewGuid(), "Entrega", Money.Create(1000, "BRL"), DateTime.UtcNow.AddDays(1), _clock);

        escrowAccount = EscrowAccount.Create(Guid.NewGuid(), agreement.AgreementId, agreement.OwnerUserId, "BRL", _clock);
        escrowAccount.RegisterDeposit(Guid.NewGuid(), Money.Create(1500, "BRL"), "seed", EscrowTransactionStatus.Completed, "pi", null, _clock);
        escrowAccount.ClearDomainEvents();

        agreement.AttachEscrowAccount(escrowAccount.EscrowAccountId);
        agreement.ClearDomainEvents();

        return agreement;
    }
}
