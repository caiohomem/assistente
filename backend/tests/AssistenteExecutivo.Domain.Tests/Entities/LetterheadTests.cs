using AssistenteExecutivo.Domain.DomainEvents;
using AssistenteExecutivo.Domain.Entities;
using AssistenteExecutivo.Domain.Exceptions;
using AssistenteExecutivo.Domain.Interfaces;
using FluentAssertions;

namespace AssistenteExecutivo.Domain.Tests.Entities;

public class LetterheadTests
{
    private readonly IClock _clock;

    public LetterheadTests()
    {
        _clock = new TestClock();
    }

    [Fact]
    public void Constructor_ValidData_ShouldCreate()
    {
        // Arrange
        var letterheadId = Guid.NewGuid();
        var ownerUserId = Guid.NewGuid();
        var name = "Company Letterhead";
        var designData = "{\"logo\":\"url\",\"colors\":[\"#000\"]}";

        // Act
        var letterhead = new Letterhead(letterheadId, ownerUserId, name, designData, _clock);

        // Assert
        letterhead.LetterheadId.Should().Be(letterheadId);
        letterhead.OwnerUserId.Should().Be(ownerUserId);
        letterhead.Name.Should().Be(name);
        letterhead.DesignData.Should().Be(designData);
        letterhead.IsActive.Should().BeTrue();
    }

    [Fact]
    public void Constructor_EmptyLetterheadId_ShouldThrow()
    {
        // Act & Assert
        var act = () => new Letterhead(Guid.Empty, Guid.NewGuid(), "Letterhead", "{}", _clock);
        act.Should().Throw<DomainException>()
            .WithMessage("*LetterheadIdObrigatorio*");
    }

    [Fact]
    public void Constructor_EmptyOwnerUserId_ShouldThrow()
    {
        // Act & Assert
        var act = () => new Letterhead(Guid.NewGuid(), Guid.Empty, "Letterhead", "{}", _clock);
        act.Should().Throw<DomainException>()
            .WithMessage("*OwnerUserIdObrigatorio*");
    }

    [Fact]
    public void Constructor_EmptyName_ShouldThrow()
    {
        // Act & Assert
        var act = () => new Letterhead(Guid.NewGuid(), Guid.NewGuid(), "", "{}", _clock);
        act.Should().Throw<DomainException>()
            .WithMessage("*LetterheadNameObrigatorio*");
    }

    [Fact]
    public void Constructor_EmptyDesignData_ShouldThrow()
    {
        // Act & Assert
        var act = () => new Letterhead(Guid.NewGuid(), Guid.NewGuid(), "Letterhead", "", _clock);
        act.Should().Throw<DomainException>()
            .WithMessage("*LetterheadDesignDataObrigatorio*");
    }

    [Fact]
    public void Constructor_NullClock_ShouldThrow()
    {
        // Act & Assert
        var act = () => new Letterhead(Guid.NewGuid(), Guid.NewGuid(), "Letterhead", "{}", null!);
        act.Should().Throw<DomainException>()
            .WithMessage("*ClockObrigatorio*");
    }

    [Fact]
    public void Constructor_NameWithWhitespace_ShouldTrim()
    {
        // Arrange
        var letterheadId = Guid.NewGuid();
        var ownerUserId = Guid.NewGuid();

        // Act
        var letterhead = new Letterhead(letterheadId, ownerUserId, "  Letterhead  ", "{}", _clock);

        // Assert
        letterhead.Name.Should().Be("Letterhead");
    }

    [Fact]
    public void Create_ValidData_ShouldCreateAndEmitEvent()
    {
        // Arrange
        var letterheadId = Guid.NewGuid();
        var ownerUserId = Guid.NewGuid();
        var name = "Company Letterhead";
        var designData = "{\"logo\":\"url\"}";

        // Act
        var letterhead = Letterhead.Create(letterheadId, ownerUserId, name, designData, _clock);

        // Assert
        letterhead.LetterheadId.Should().Be(letterheadId);
        letterhead.DomainEvents.Should().ContainSingle()
            .Which.Should().BeOfType<LetterheadCreated>();
    }

    [Fact]
    public void UpdateName_ValidName_ShouldUpdate()
    {
        // Arrange
        var letterhead = CreateLetterhead();
        var newName = "New Letterhead Name";

        // Act
        letterhead.UpdateName(newName, _clock);

        // Assert
        letterhead.Name.Should().Be(newName);
        letterhead.UpdatedAt.Should().BeCloseTo(_clock.UtcNow, TimeSpan.FromSeconds(1));
    }

    [Fact]
    public void UpdateName_EmptyName_ShouldThrow()
    {
        // Arrange
        var letterhead = CreateLetterhead();

        // Act & Assert
        var act = () => letterhead.UpdateName("", _clock);
        act.Should().Throw<DomainException>()
            .WithMessage("*LetterheadNameObrigatorio*");
    }

    [Fact]
    public void UpdateName_WithWhitespace_ShouldTrim()
    {
        // Arrange
        var letterhead = CreateLetterhead();

        // Act
        letterhead.UpdateName("  New Name  ", _clock);

        // Assert
        letterhead.Name.Should().Be("New Name");
    }

    [Fact]
    public void UpdateDesignData_ValidData_ShouldUpdate()
    {
        // Arrange
        var letterhead = CreateLetterhead();
        var newDesignData = "{\"logo\":\"new-url\",\"colors\":[\"#FFF\"]}";

        // Act
        letterhead.UpdateDesignData(newDesignData, _clock);

        // Assert
        letterhead.DesignData.Should().Be(newDesignData);
        letterhead.UpdatedAt.Should().BeCloseTo(_clock.UtcNow, TimeSpan.FromSeconds(1));
    }

    [Fact]
    public void UpdateDesignData_EmptyData_ShouldThrow()
    {
        // Arrange
        var letterhead = CreateLetterhead();

        // Act & Assert
        var act = () => letterhead.UpdateDesignData("", _clock);
        act.Should().Throw<DomainException>()
            .WithMessage("*LetterheadDesignDataObrigatorio*");
    }

    [Fact]
    public void Activate_WhenInactive_ShouldActivate()
    {
        // Arrange
        var letterhead = CreateLetterhead();
        letterhead.Deactivate(_clock);

        // Act
        letterhead.Activate(_clock);

        // Assert
        letterhead.IsActive.Should().BeTrue();
        letterhead.UpdatedAt.Should().BeCloseTo(_clock.UtcNow, TimeSpan.FromSeconds(1));
    }

    [Fact]
    public void Activate_WhenActive_ShouldNotUpdate()
    {
        // Arrange
        var letterhead = CreateLetterhead();
        var originalUpdatedAt = letterhead.UpdatedAt;
        Thread.Sleep(10); // Small delay to ensure time difference

        // Act
        letterhead.Activate(_clock);

        // Assert
        letterhead.IsActive.Should().BeTrue();
        // UpdatedAt should not change if already active
        letterhead.UpdatedAt.Should().BeCloseTo(originalUpdatedAt, TimeSpan.FromSeconds(1));
    }

    [Fact]
    public void Deactivate_WhenActive_ShouldDeactivate()
    {
        // Arrange
        var letterhead = CreateLetterhead();

        // Act
        letterhead.Deactivate(_clock);

        // Assert
        letterhead.IsActive.Should().BeFalse();
        letterhead.UpdatedAt.Should().BeCloseTo(_clock.UtcNow, TimeSpan.FromSeconds(1));
    }

    [Fact]
    public void Deactivate_WhenInactive_ShouldNotUpdate()
    {
        // Arrange
        var letterhead = CreateLetterhead();
        letterhead.Deactivate(_clock);
        var originalUpdatedAt = letterhead.UpdatedAt;
        Thread.Sleep(10); // Small delay to ensure time difference

        // Act
        letterhead.Deactivate(_clock);

        // Assert
        letterhead.IsActive.Should().BeFalse();
        // UpdatedAt should not change if already inactive
        letterhead.UpdatedAt.Should().BeCloseTo(originalUpdatedAt, TimeSpan.FromSeconds(1));
    }

    private Letterhead CreateLetterhead()
    {
        return new Letterhead(
            Guid.NewGuid(),
            Guid.NewGuid(),
            "Test Letterhead",
            "{\"logo\":\"url\"}",
            _clock);
    }

    private class TestClock : IClock
    {
        public DateTime UtcNow => DateTime.UtcNow;
    }
}



