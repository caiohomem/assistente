using AssistenteExecutivo.Application.Commands.Credits;
using AssistenteExecutivo.Application.Queries.Credits;
using AssistenteExecutivo.Application.Tests.Helpers;
using FluentAssertions;

namespace AssistenteExecutivo.Application.Tests.Handlers.Credits;

public class GrantCreditsCommandHandlerTests : HandlerTestBase
{
    [Fact]
    public async Task Handle_ValidGrant_ShouldIncreaseBalance()
    {
        // Arrange
        var ownerUserId = Guid.NewGuid();
        var command = new GrantCreditsCommand
        {
            OwnerUserId = ownerUserId,
            Amount = 100.50m,
            Reason = "Initial credit grant"
        };

        // Act
        var result = await SendAsync(command);

        // Assert
        result.Should().NotBeNull();
        result.OwnerUserId.Should().Be(ownerUserId);
        result.NewBalance.Should().Be(100.50m);
        result.TransactionId.Should().NotBeEmpty();

        // Verify balance via query
        var balanceQuery = new GetCreditBalanceQuery
        {
            OwnerUserId = ownerUserId
        };
        var balance = await SendAsync(balanceQuery);

        balance.Should().NotBeNull();
        balance!.Balance.Should().Be(100.50m);
    }

    [Fact]
    public async Task Handle_MultipleGrants_ShouldAccumulateBalance()
    {
        // Arrange
        var ownerUserId = Guid.NewGuid();

        // Act
        var grant1 = await SendAsync(new GrantCreditsCommand
        {
            OwnerUserId = ownerUserId,
            Amount = 50m,
            Reason = "First grant"
        });

        var grant2 = await SendAsync(new GrantCreditsCommand
        {
            OwnerUserId = ownerUserId,
            Amount = 75.25m,
            Reason = "Second grant"
        });

        // Assert
        grant1.NewBalance.Should().Be(50m);
        grant2.NewBalance.Should().Be(125.25m);

        var balanceQuery = new GetCreditBalanceQuery
        {
            OwnerUserId = ownerUserId
        };
        var balance = await SendAsync(balanceQuery);

        balance.Should().NotBeNull();
        balance!.Balance.Should().Be(125.25m);
    }

    [Fact]
    public async Task Handle_EmptyOwnerUserId_ShouldThrowException()
    {
        // Arrange
        var command = new GrantCreditsCommand
        {
            OwnerUserId = Guid.Empty,
            Amount = 100m
        };

        // Act & Assert
        var act = async () => await SendAsync(command);
        await act.Should().ThrowAsync<ArgumentException>()
            .WithMessage("*OwnerUserId*");
    }

    [Fact]
    public async Task Handle_ZeroAmount_ShouldThrowException()
    {
        // Arrange
        var ownerUserId = Guid.NewGuid();
        var command = new GrantCreditsCommand
        {
            OwnerUserId = ownerUserId,
            Amount = 0m
        };

        // Act & Assert
        var act = async () => await SendAsync(command);
        await act.Should().ThrowAsync<ArgumentException>()
            .WithMessage("*Amount*");
    }

    [Fact]
    public async Task Handle_NegativeAmount_ShouldThrowException()
    {
        // Arrange
        var ownerUserId = Guid.NewGuid();
        var command = new GrantCreditsCommand
        {
            OwnerUserId = ownerUserId,
            Amount = -10m
        };

        // Act & Assert
        var act = async () => await SendAsync(command);
        await act.Should().ThrowAsync<ArgumentException>()
            .WithMessage("*Amount*");
    }
}












