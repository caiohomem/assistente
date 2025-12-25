using AssistenteExecutivo.Domain.Exceptions;
using AssistenteExecutivo.Domain.ValueObjects;
using FluentAssertions;

namespace AssistenteExecutivo.Domain.Tests.ValueObjects;

public class MediaRefTests
{
    [Fact]
    public void Create_ValidData_ShouldSucceed()
    {
        // Act
        var mediaRef = MediaRef.Create("key-123", "hash-456", "image/jpeg", 1024);

        // Assert
        mediaRef.StorageKey.Should().Be("key-123");
        mediaRef.Hash.Should().Be("hash-456");
        mediaRef.MimeType.Should().Be("image/jpeg");
        mediaRef.SizeBytes.Should().Be(1024);
    }

    [Fact]
    public void Create_EmptyStorageKey_ShouldThrow()
    {
        // Act & Assert
        var act = () => MediaRef.Create("", "hash", "image/jpeg", 1024);
        act.Should().Throw<DomainException>()
            .WithMessage("*StorageKeyObrigatorio*");
    }

    [Fact]
    public void Create_EmptyHash_ShouldThrow()
    {
        // Act & Assert
        var act = () => MediaRef.Create("key", "", "image/jpeg", 1024);
        act.Should().Throw<DomainException>()
            .WithMessage("*HashObrigatorio*");
    }

    [Fact]
    public void Create_EmptyMimeType_ShouldThrow()
    {
        // Act & Assert
        var act = () => MediaRef.Create("key", "hash", "", 1024);
        act.Should().Throw<DomainException>()
            .WithMessage("*MimeTypeObrigatorio*");
    }

    [Fact]
    public void Create_ZeroSize_ShouldThrow()
    {
        // Act & Assert
        var act = () => MediaRef.Create("key", "hash", "image/jpeg", 0);
        act.Should().Throw<DomainException>()
            .WithMessage("*TamanhoArquivoInvalido*");
    }

    [Fact]
    public void Create_NegativeSize_ShouldThrow()
    {
        // Act & Assert
        var act = () => MediaRef.Create("key", "hash", "image/jpeg", -1);
        act.Should().Throw<DomainException>()
            .WithMessage("*TamanhoArquivoInvalido*");
    }

    [Fact]
    public void Equals_SameStorageKeyAndHash_ShouldBeEqual()
    {
        // Arrange
        var mediaRef1 = MediaRef.Create("key-123", "hash-456", "image/jpeg", 1024);
        var mediaRef2 = MediaRef.Create("key-123", "hash-456", "image/png", 2048);

        // Assert
        mediaRef1.Should().Be(mediaRef2);
    }

    [Fact]
    public void Equals_DifferentStorageKey_ShouldNotBeEqual()
    {
        // Arrange
        var mediaRef1 = MediaRef.Create("key-123", "hash-456", "image/jpeg", 1024);
        var mediaRef2 = MediaRef.Create("key-789", "hash-456", "image/jpeg", 1024);

        // Assert
        mediaRef1.Should().NotBe(mediaRef2);
    }
}

