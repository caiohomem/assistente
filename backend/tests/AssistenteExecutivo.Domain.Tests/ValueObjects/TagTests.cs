using AssistenteExecutivo.Domain.Exceptions;
using AssistenteExecutivo.Domain.ValueObjects;
using FluentAssertions;

namespace AssistenteExecutivo.Domain.Tests.ValueObjects;

public class TagTests
{
    [Fact]
    public void Create_ValidTag_ShouldSucceed()
    {
        // Act
        var tag = Tag.Create("importante");

        // Assert
        tag.Value.Should().Be("importante");
    }

    [Fact]
    public void Create_TagWithWhitespace_ShouldNormalize()
    {
        // Act
        var tag = Tag.Create("  IMPORTANTE  ");

        // Assert
        tag.Value.Should().Be("importante");
    }

    [Fact]
    public void Create_TagWithUpperCase_ShouldNormalize()
    {
        // Act
        var tag = Tag.Create("IMPORTANTE");

        // Assert
        tag.Value.Should().Be("importante");
    }

    [Fact]
    public void Create_EmptyTag_ShouldThrow()
    {
        // Act & Assert
        var act = () => Tag.Create("");
        act.Should().Throw<DomainException>()
            .WithMessage("*TagObrigatoria*");
    }

    [Fact]
    public void Create_TagTooLong_ShouldThrow()
    {
        // Arrange
        var longTag = new string('a', 51);

        // Act & Assert
        var act = () => Tag.Create(longTag);
        act.Should().Throw<DomainException>()
            .WithMessage("*TagMaximoCaracteres*");
    }

    [Fact]
    public void Equals_SameTag_ShouldBeEqual()
    {
        // Arrange
        var tag1 = Tag.Create("importante");
        var tag2 = Tag.Create("IMPORTANTE");

        // Assert
        tag1.Should().Be(tag2);
    }
}

