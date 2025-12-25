using AssistenteExecutivo.Application.Interfaces;
using AssistenteExecutivo.Domain.Entities;
using AssistenteExecutivo.Domain.Enums;
using AssistenteExecutivo.Domain.ValueObjects;
using AssistenteExecutivo.Infrastructure.Repositories;
using FluentAssertions;

namespace AssistenteExecutivo.Infrastructure.Tests.Repositories;

public class MediaAssetRepositoryTests : RepositoryTestBase
{
    private readonly IMediaAssetRepository _repository;

    public MediaAssetRepositoryTests()
    {
        _repository = new MediaAssetRepository(Context);
    }

    [Fact]
    public async Task GetByIdAsync_ExistingMediaAsset_ShouldReturnMediaAsset()
    {
        // Arrange
        var ownerUserId = Guid.NewGuid();
        var mediaId = Guid.NewGuid();
        var mediaRef = MediaRef.Create("storage-key-123", "hash-456", "image/jpeg", 1024);
        var mediaAsset = new MediaAsset(mediaId, ownerUserId, mediaRef, MediaKind.Image, Clock);
        mediaAsset.Metadata["key"] = "value"; // Initialize Metadata dictionary
        
        await Context.MediaAssets.AddAsync(mediaAsset);
        await SaveChangesAsync();

        // Act
        var result = await _repository.GetByIdAsync(mediaId, ownerUserId);

        // Assert
        result.Should().NotBeNull();
        result!.MediaId.Should().Be(mediaId);
        result.OwnerUserId.Should().Be(ownerUserId);
        result.MediaRef.Hash.Should().Be("hash-456");
    }

    [Fact]
    public async Task GetByIdAsync_NonExistentMediaAsset_ShouldReturnNull()
    {
        // Arrange
        var ownerUserId = Guid.NewGuid();
        var mediaId = Guid.NewGuid();

        // Act
        var result = await _repository.GetByIdAsync(mediaId, ownerUserId);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task GetByIdAsync_DifferentOwner_ShouldReturnNull()
    {
        // Arrange
        var ownerUserId1 = Guid.NewGuid();
        var ownerUserId2 = Guid.NewGuid();
        var mediaId = Guid.NewGuid();
        var mediaRef = MediaRef.Create("storage-key-123", "hash-456", "image/jpeg", 1024);
        var mediaAsset = new MediaAsset(mediaId, ownerUserId1, mediaRef, MediaKind.Image, Clock);
        mediaAsset.Metadata["key"] = "value"; // Initialize Metadata dictionary
        
        await Context.MediaAssets.AddAsync(mediaAsset);
        await SaveChangesAsync();

        // Act
        var result = await _repository.GetByIdAsync(mediaId, ownerUserId2);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task GetByHashAsync_ExistingHash_ShouldReturnMediaAsset()
    {
        // Arrange
        var ownerUserId = Guid.NewGuid();
        var mediaRef = MediaRef.Create("storage-key-123", "hash-456", "image/jpeg", 1024);
        var mediaAsset = new MediaAsset(Guid.NewGuid(), ownerUserId, mediaRef, MediaKind.Image, Clock);
        mediaAsset.Metadata["key"] = "value"; // Initialize Metadata dictionary
        
        await Context.MediaAssets.AddAsync(mediaAsset);
        await SaveChangesAsync();

        // Act
        var result = await _repository.GetByHashAsync("hash-456", ownerUserId);

        // Assert
        result.Should().NotBeNull();
        result!.MediaRef.Hash.Should().Be("hash-456");
    }

    [Fact]
    public async Task GetByHashAsync_NonExistentHash_ShouldReturnNull()
    {
        // Arrange
        var ownerUserId = Guid.NewGuid();
        var mediaRef = MediaRef.Create("storage-key-123", "hash-456", "image/jpeg", 1024);
        var mediaAsset = new MediaAsset(Guid.NewGuid(), ownerUserId, mediaRef, MediaKind.Image, Clock);
        mediaAsset.Metadata["key"] = "value"; // Initialize Metadata dictionary
        
        await Context.MediaAssets.AddAsync(mediaAsset);
        await SaveChangesAsync();

        // Act
        var result = await _repository.GetByHashAsync("different-hash", ownerUserId);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task GetAllByOwnerAsync_ShouldReturnAllMediaAssetsForOwner()
    {
        // Arrange
        var ownerUserId1 = Guid.NewGuid();
        var ownerUserId2 = Guid.NewGuid();
        
        var mediaRef1 = MediaRef.Create("storage-key-1", "hash-1", "image/jpeg", 1024);
        var mediaRef2 = MediaRef.Create("storage-key-2", "hash-2", "image/png", 2048);
        var mediaRef3 = MediaRef.Create("storage-key-3", "hash-3", "audio/mpeg", 4096);
        
        var mediaAsset1 = new MediaAsset(Guid.NewGuid(), ownerUserId1, mediaRef1, MediaKind.Image, Clock);
        var mediaAsset2 = new MediaAsset(Guid.NewGuid(), ownerUserId1, mediaRef2, MediaKind.Image, Clock);
        var mediaAsset3 = new MediaAsset(Guid.NewGuid(), ownerUserId2, mediaRef3, MediaKind.Audio, Clock);
        mediaAsset1.Metadata["key"] = "value"; // Initialize Metadata dictionary
        mediaAsset2.Metadata["key"] = "value"; // Initialize Metadata dictionary
        mediaAsset3.Metadata["key"] = "value"; // Initialize Metadata dictionary
        
        await Context.MediaAssets.AddRangeAsync(mediaAsset1, mediaAsset2, mediaAsset3);
        await SaveChangesAsync();

        // Act
        var result = await _repository.GetAllByOwnerAsync(ownerUserId1);

        // Assert
        result.Should().HaveCount(2);
        result.Should().Contain(m => m.MediaId == mediaAsset1.MediaId);
        result.Should().Contain(m => m.MediaId == mediaAsset2.MediaId);
        result.Should().NotContain(m => m.MediaId == mediaAsset3.MediaId);
    }

    [Fact]
    public async Task AddAsync_ShouldAddMediaAsset()
    {
        // Arrange
        var mediaRef = MediaRef.Create("storage-key-123", "hash-456", "image/jpeg", 1024);
        var mediaAsset = new MediaAsset(Guid.NewGuid(), Guid.NewGuid(), mediaRef, MediaKind.Image, Clock);
        mediaAsset.Metadata["key"] = "value"; // Initialize Metadata dictionary

        // Act
        await _repository.AddAsync(mediaAsset);
        await SaveChangesAsync();

        // Assert
        var result = await Context.MediaAssets.FindAsync(mediaAsset.MediaId);
        result.Should().NotBeNull();
        result!.MediaRef.Hash.Should().Be("hash-456");
    }

    [Fact]
    public async Task DeleteAsync_ShouldDeleteMediaAsset()
    {
        // Arrange
        var mediaRef = MediaRef.Create("storage-key-123", "hash-456", "image/jpeg", 1024);
        var mediaAsset = new MediaAsset(Guid.NewGuid(), Guid.NewGuid(), mediaRef, MediaKind.Image, Clock);
        mediaAsset.Metadata["key"] = "value"; // Initialize Metadata dictionary
        
        await Context.MediaAssets.AddAsync(mediaAsset);
        await SaveChangesAsync();

        // Act
        await _repository.DeleteAsync(mediaAsset);
        await SaveChangesAsync();

        // Assert
        var result = await Context.MediaAssets.FindAsync(mediaAsset.MediaId);
        result.Should().BeNull();
    }
}

