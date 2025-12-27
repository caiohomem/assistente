using AssistenteExecutivo.Application.Commands.Credits;
using AssistenteExecutivo.Application.Queries.Credits;
using AssistenteExecutivo.Application.Tests.Helpers;
using FluentAssertions;

namespace AssistenteExecutivo.Application.Tests.Handlers.Credits;

public class ConsumeCreditsCommandHandlerTests : HandlerTestBase
{
    [Fact]
    public async Task Handle_ValidConsume_ShouldDecreaseBalance()
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

        var command = new ConsumeCreditsCommand
        {
            OwnerUserId = ownerUserId,
            Amount = 30m,
            IdempotencyKey = Guid.NewGuid().ToString(),
            Purpose = "Test consumption"
        };

        // Act
        var result = await SendAsync(command);

        // Assert
        result.Should().NotBeNull();
        result.OwnerUserId.Should().Be(ownerUserId);
        result.NewBalance.Should().Be(70m);
        result.TransactionId.Should().NotBeEmpty();
        result.WasIdempotent.Should().BeFalse();

        // Verify balance
        var balanceQuery = new GetCreditBalanceQuery
        {
            OwnerUserId = ownerUserId
        };
        var balance = await SendAsync(balanceQuery);

        balance.Should().NotBeNull();
        balance!.Balance.Should().Be(70m);
    }

    [Fact]
    public async Task Handle_DuplicateIdempotencyKey_ShouldReturnIdempotentResult()
    {
        // Arrange
        var ownerUserId = Guid.NewGuid();
        var idempotencyKey = Guid.NewGuid().ToString();

        // Grant credits first
        await SendAsync(new GrantCreditsCommand
        {
            OwnerUserId = ownerUserId,
            Amount = 100m,
            Reason = "Initial grant"
        });

        var command1 = new ConsumeCreditsCommand
        {
            OwnerUserId = ownerUserId,
            Amount = 30m,
            IdempotencyKey = idempotencyKey,
            Purpose = "Test consumption"
        };

        // First consume
        var result1 = await SendAsync(command1);
        result1.NewBalance.Should().Be(70m);
        result1.WasIdempotent.Should().BeFalse();

        // Act - Second consume with same idempotency key
        var command2 = new ConsumeCreditsCommand
        {
            OwnerUserId = ownerUserId,
            Amount = 30m,
            IdempotencyKey = idempotencyKey, // Same key
            Purpose = "Test consumption"
        };
        var result2 = await SendAsync(command2);

        // Assert
        result2.Should().NotBeNull();
        result2.WasIdempotent.Should().BeTrue();
        result2.NewBalance.Should().Be(70m); // Balance should not change
        result2.TransactionId.Should().Be(result1.TransactionId); // Same transaction

        // Verify balance didn't change
        var balanceQuery = new GetCreditBalanceQuery
        {
            OwnerUserId = ownerUserId
        };
        var balance = await SendAsync(balanceQuery);

        balance.Should().NotBeNull();
        balance!.Balance.Should().Be(70m);
    }

    [Fact]
    public async Task Handle_InsufficientBalance_ShouldThrowException()
    {
        // Arrange
        var ownerUserId = Guid.NewGuid();

        // Grant only 50 credits
        await SendAsync(new GrantCreditsCommand
        {
            OwnerUserId = ownerUserId,
            Amount = 50m,
            Reason = "Initial grant"
        });

        var command = new ConsumeCreditsCommand
        {
            OwnerUserId = ownerUserId,
            Amount = 100m, // More than available
            IdempotencyKey = Guid.NewGuid().ToString(),
            Purpose = "Test consumption"
        };

        // Act & Assert
        var act = async () => await SendAsync(command);
        await act.Should().ThrowAsync<Domain.Exceptions.DomainException>();
    }

    [Fact]
    public async Task Handle_EmptyOwnerUserId_ShouldThrowException()
    {
        // Arrange
        var command = new ConsumeCreditsCommand
        {
            OwnerUserId = Guid.Empty,
            Amount = 10m,
            IdempotencyKey = Guid.NewGuid().ToString(),
            Purpose = "Test"
        };

        // Act & Assert
        var act = async () => await SendAsync(command);
        await act.Should().ThrowAsync<ArgumentException>()
            .WithMessage("*OwnerUserId*");
    }

    [Fact]
    public async Task Handle_EmptyIdempotencyKey_ShouldThrowException()
    {
        // Arrange
        var ownerUserId = Guid.NewGuid();
        var command = new ConsumeCreditsCommand
        {
            OwnerUserId = ownerUserId,
            Amount = 10m,
            IdempotencyKey = string.Empty,
            Purpose = "Test"
        };

        // Act & Assert
        var act = async () => await SendAsync(command);
        await act.Should().ThrowAsync<ArgumentException>()
            .WithMessage("*IdempotencyKey*");
    }
}






