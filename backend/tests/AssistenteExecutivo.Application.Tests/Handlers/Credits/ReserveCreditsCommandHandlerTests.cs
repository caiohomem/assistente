using AssistenteExecutivo.Application.Commands.Credits;
using AssistenteExecutivo.Application.Queries.Credits;
using AssistenteExecutivo.Application.Tests.Helpers;
using FluentAssertions;

namespace AssistenteExecutivo.Application.Tests.Handlers.Credits;

public class ReserveCreditsCommandHandlerTests : HandlerTestBase
{
    [Fact]
    public async Task Handle_ValidReserve_ShouldReserveCredits()
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

        var command = new ReserveCreditsCommand
        {
            OwnerUserId = ownerUserId,
            Amount = 40m,
            IdempotencyKey = Guid.NewGuid().ToString(),
            Purpose = "Reserve for processing"
        };

        // Act
        var result = await SendAsync(command);

        // Assert
        result.Should().NotBeNull();
        result.OwnerUserId.Should().Be(ownerUserId);
        result.NewBalance.Should().Be(60m); // 100 - 40 reserved
        result.TransactionId.Should().NotBeEmpty();
        result.WasIdempotent.Should().BeFalse();

        // Verify balance
        var balanceQuery = new GetCreditBalanceQuery
        {
            OwnerUserId = ownerUserId
        };
        var balance = await SendAsync(balanceQuery);

        balance.Should().NotBeNull();
        balance!.Balance.Should().Be(60m);
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

        var command1 = new ReserveCreditsCommand
        {
            OwnerUserId = ownerUserId,
            Amount = 30m,
            IdempotencyKey = idempotencyKey,
            Purpose = "Reserve"
        };

        var result1 = await SendAsync(command1);
        result1.WasIdempotent.Should().BeFalse();

        // Act - Second reserve with same idempotency key
        var command2 = new ReserveCreditsCommand
        {
            OwnerUserId = ownerUserId,
            Amount = 30m,
            IdempotencyKey = idempotencyKey,
            Purpose = "Reserve"
        };
        var result2 = await SendAsync(command2);

        // Assert
        result2.WasIdempotent.Should().BeTrue();
        result2.NewBalance.Should().Be(result1.NewBalance);
        result2.TransactionId.Should().Be(result1.TransactionId);
    }

    [Fact]
    public async Task Handle_InsufficientBalance_ShouldThrowException()
    {
        // Arrange
        var ownerUserId = Guid.NewGuid();

        await SendAsync(new GrantCreditsCommand
        {
            OwnerUserId = ownerUserId,
            Amount = 50m,
            Reason = "Initial grant"
        });

        var command = new ReserveCreditsCommand
        {
            OwnerUserId = ownerUserId,
            Amount = 100m,
            IdempotencyKey = Guid.NewGuid().ToString(),
            Purpose = "Reserve"
        };

        // Act & Assert
        var act = async () => await SendAsync(command);
        await act.Should().ThrowAsync<Domain.Exceptions.DomainException>();
    }
}


