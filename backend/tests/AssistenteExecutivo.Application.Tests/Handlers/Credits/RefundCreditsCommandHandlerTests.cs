using AssistenteExecutivo.Application.Commands.Credits;
using AssistenteExecutivo.Application.Queries.Credits;
using AssistenteExecutivo.Application.Tests.Helpers;
using FluentAssertions;

namespace AssistenteExecutivo.Application.Tests.Handlers.Credits;

public class RefundCreditsCommandHandlerTests : HandlerTestBase
{
    [Fact]
    public async Task Handle_ValidRefund_ShouldIncreaseBalance()
    {
        // Arrange
        var ownerUserId = Guid.NewGuid();

        // Grant credits first
        await SendAsync(new GrantCreditsCommand
        {
            OwnerUserId = ownerUserId,
            Amount = 100m,
            Reason = "Initial grant"
        });

        // Consume some credits
        await SendAsync(new ConsumeCreditsCommand
        {
            OwnerUserId = ownerUserId,
            Amount = 30m,
            IdempotencyKey = Guid.NewGuid().ToString(),
            Purpose = "Consume"
        });

        var command = new RefundCreditsCommand
        {
            OwnerUserId = ownerUserId,
            Amount = 20m,
            IdempotencyKey = Guid.NewGuid().ToString(),
            Purpose = "Refund"
        };

        // Act
        var result = await SendAsync(command);

        // Assert
        result.Should().NotBeNull();
        result.OwnerUserId.Should().Be(ownerUserId);
        result.NewBalance.Should().Be(90m); // 100 - 30 + 20 = 90
        result.TransactionId.Should().NotBeEmpty();
        result.WasIdempotent.Should().BeFalse();

        // Verify balance
        var balanceQuery = new GetCreditBalanceQuery
        {
            OwnerUserId = ownerUserId
        };
        var balance = await SendAsync(balanceQuery);

        balance.Should().NotBeNull();
        balance!.Balance.Should().Be(90m);
    }

    [Fact]
    public async Task Handle_DuplicateIdempotencyKey_ShouldReturnIdempotentResult()
    {
        // Arrange
        var ownerUserId = Guid.NewGuid();
        var idempotencyKey = Guid.NewGuid().ToString();

        await SendAsync(new GrantCreditsCommand
        {
            OwnerUserId = ownerUserId,
            Amount = 100m,
            Reason = "Initial grant"
        });

        var command1 = new RefundCreditsCommand
        {
            OwnerUserId = ownerUserId,
            Amount = 20m,
            IdempotencyKey = idempotencyKey,
            Purpose = "Refund"
        };

        var result1 = await SendAsync(command1);
        result1.WasIdempotent.Should().BeFalse();

        // Act - Second refund with same idempotency key
        var command2 = new RefundCreditsCommand
        {
            OwnerUserId = ownerUserId,
            Amount = 20m,
            IdempotencyKey = idempotencyKey,
            Purpose = "Refund"
        };
        var result2 = await SendAsync(command2);

        // Assert
        result2.WasIdempotent.Should().BeTrue();
        result2.NewBalance.Should().Be(result1.NewBalance);
        result2.TransactionId.Should().Be(result1.TransactionId);
    }
}










