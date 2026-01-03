using AssistenteExecutivo.Application.Commands.Escrow;
using AssistenteExecutivo.Application.Handlers.Escrow;
using AssistenteExecutivo.Application.Interfaces;
using AssistenteExecutivo.Application.Tests.Helpers;
using AssistenteExecutivo.Domain.Entities;
using AssistenteExecutivo.Domain.Interfaces;
using AssistenteExecutivo.Domain.ValueObjects;
using FluentAssertions;
using MediatR;
using Moq;

namespace AssistenteExecutivo.Application.Tests.Handlers.Escrow;

public class DepositToEscrowCommandHandlerTests
{
    private readonly Mock<IEscrowAccountRepository> _escrowRepository = new();
    private readonly Mock<IPaymentGateway> _paymentGateway = new();
    private readonly Mock<IUnitOfWork> _unitOfWork = new();
    private readonly Mock<IPublisher> _publisher = new();
    private readonly TestClock _clock = new(DateTime.UtcNow);

    private readonly DepositToEscrowCommandHandler _handler;

    public DepositToEscrowCommandHandlerTests()
    {
        _handler = new DepositToEscrowCommandHandler(
            _escrowRepository.Object,
            _paymentGateway.Object,
            _unitOfWork.Object,
            _clock,
            _publisher.Object);
    }

    [Fact]
    public async Task Handle_ShouldCreatePaymentIntentAndPersistDeposit()
    {
        var account = EscrowAccount.Create(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), "BRL", _clock);
        account.ClearDomainEvents();

        _escrowRepository.Setup(r => r.GetByIdAsync(account.EscrowAccountId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(account);

        _paymentGateway.Setup(g => g.CreateEscrowDepositIntentAsync(
                account.EscrowAccountId,
                It.IsAny<Money>(),
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new PaymentIntentResult
            {
                PaymentIntentId = "pi_test",
                ClientSecret = "secret"
            });

        _unitOfWork.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);

        var command = new DepositToEscrowCommand
        {
            EscrowAccountId = account.EscrowAccountId,
            TransactionId = Guid.NewGuid(),
            Amount = 250,
            Currency = "BRL",
            RequestedBy = account.OwnerUserId
        };

        var result = await _handler.Handle(command, CancellationToken.None);

        result.PaymentIntentId.Should().Be("pi_test");
        account.Transactions.Should().HaveCount(1);
        _paymentGateway.Verify(g => g.CreateEscrowDepositIntentAsync(
            account.EscrowAccountId,
            It.Is<Money>(m => m.Amount == 250),
            It.IsAny<string>(),
            It.IsAny<CancellationToken>()), Times.Once);
        _unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }
}
