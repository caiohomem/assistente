using AssistenteExecutivo.Domain.Entities;
using AssistenteExecutivo.Domain.Enums;
using AssistenteExecutivo.Domain.Exceptions;
using AssistenteExecutivo.Domain.Interfaces;
using AssistenteExecutivo.Domain.ValueObjects;
using FluentAssertions;

namespace AssistenteExecutivo.Domain.Tests.Entities;

public class MediaAssetTests
{
    private readonly IClock _clock;

    public MediaAssetTests()
    {
        _clock = new TestClock();
    }

    [Fact]
    public void Constructor_ValidData_ShouldCreate()
    {
        // Arrange
        var mediaId = Guid.NewGuid();
        var ownerUserId = Guid.NewGuid();
        var mediaRef = MediaRef.Create("key-123", "hash-456", "image/jpeg", 1024);

        // Act
        var mediaAsset = new MediaAsset(mediaId, ownerUserId, mediaRef, MediaKind.Image, _clock);

        // Assert
        mediaAsset.MediaId.Should().Be(mediaId);
        mediaAsset.OwnerUserId.Should().Be(ownerUserId);
        mediaAsset.MediaRef.Should().Be(mediaRef);
        mediaAsset.Kind.Should().Be(MediaKind.Image);
        mediaAsset.Metadata.Should().BeEmpty();
    }

    [Fact]
    public void Constructor_EmptyMediaId_ShouldThrow()
    {
        // Arrange
        var ownerUserId = Guid.NewGuid();
        var mediaRef = MediaRef.Create("key", "hash", "image/jpeg", 1024);

        // Act & Assert
        var act = () => new MediaAsset(Guid.Empty, ownerUserId, mediaRef, MediaKind.Image, _clock);
        act.Should().Throw<DomainException>()
            .WithMessage("*MediaIdObrigatorio*");
    }

    [Fact]
    public void Constructor_EmptyOwnerUserId_ShouldThrow()
    {
        // Arrange
        var mediaId = Guid.NewGuid();
        var mediaRef = MediaRef.Create("key", "hash", "image/jpeg", 1024);

        // Act & Assert
        var act = () => new MediaAsset(mediaId, Guid.Empty, mediaRef, MediaKind.Image, _clock);
        act.Should().Throw<DomainException>()
            .WithMessage("*OwnerUserIdObrigatorio*");
    }

    [Fact]
    public void Constructor_NullMediaRef_ShouldThrow()
    {
        // Arrange
        var mediaId = Guid.NewGuid();
        var ownerUserId = Guid.NewGuid();

        // Act & Assert
        var act = () => new MediaAsset(mediaId, ownerUserId, null!, MediaKind.Image, _clock);
        act.Should().Throw<DomainException>()
            .WithMessage("*MediaRefObrigatorio*");
    }

    private class TestClock : IClock
    {
        public DateTime UtcNow => DateTime.UtcNow;
    }
}

