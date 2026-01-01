using AssistenteExecutivo.Domain.Entities;
using AssistenteExecutivo.Domain.Exceptions;
using FluentAssertions;

namespace AssistenteExecutivo.Domain.Tests.Entities;

public class RelationshipTests
{
    [Fact]
    public void Constructor_ValidData_ShouldCreate()
    {
        // Arrange
        var relationshipId = Guid.NewGuid();
        var sourceContactId = Guid.NewGuid();
        var targetContactId = Guid.NewGuid();
        var type = "colleague";
        var description = "Works at same company";

        // Act
        var relationship = new Relationship(relationshipId, sourceContactId, targetContactId, type, description);

        // Assert
        relationship.RelationshipId.Should().Be(relationshipId);
        relationship.SourceContactId.Should().Be(sourceContactId);
        relationship.TargetContactId.Should().Be(targetContactId);
        relationship.Type.Should().Be(type);
        relationship.Description.Should().Be(description);
        relationship.Strength.Should().Be(0.0f);
        relationship.IsConfirmed.Should().BeFalse();
    }

    [Fact]
    public void Constructor_WithoutDescription_ShouldCreate()
    {
        // Arrange
        var relationshipId = Guid.NewGuid();
        var sourceContactId = Guid.NewGuid();
        var targetContactId = Guid.NewGuid();
        var type = "friend";

        // Act
        var relationship = new Relationship(relationshipId, sourceContactId, targetContactId, type);

        // Assert
        relationship.Description.Should().BeNull();
    }

    [Fact]
    public void Constructor_EmptyRelationshipId_ShouldThrow()
    {
        // Act & Assert
        var act = () => new Relationship(Guid.Empty, Guid.NewGuid(), Guid.NewGuid(), "colleague");
        act.Should().Throw<DomainException>()
            .WithMessage("*RelationshipIdObrigatorio*");
    }

    [Fact]
    public void Constructor_EmptySourceContactId_ShouldThrow()
    {
        // Act & Assert
        var act = () => new Relationship(Guid.NewGuid(), Guid.Empty, Guid.NewGuid(), "colleague");
        act.Should().Throw<DomainException>()
            .WithMessage("*SourceContactIdObrigatorio*");
    }

    [Fact]
    public void Constructor_EmptyTargetContactId_ShouldThrow()
    {
        // Act & Assert
        var act = () => new Relationship(Guid.NewGuid(), Guid.NewGuid(), Guid.Empty, "colleague");
        act.Should().Throw<DomainException>()
            .WithMessage("*TargetContactIdObrigatorio*");
    }

    [Fact]
    public void Constructor_SameSourceAndTarget_ShouldThrow()
    {
        // Arrange
        var contactId = Guid.NewGuid();

        // Act & Assert
        var act = () => new Relationship(Guid.NewGuid(), contactId, contactId, "colleague");
        act.Should().Throw<DomainException>()
            .WithMessage("*RelationshipNaoPodeSerComMesmoContato*");
    }

    [Fact]
    public void Constructor_EmptyType_ShouldThrow()
    {
        // Act & Assert
        var act = () => new Relationship(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), "");
        act.Should().Throw<DomainException>()
            .WithMessage("*RelationshipTypeObrigatorio*");
    }

    [Fact]
    public void Constructor_WhitespaceType_ShouldThrow()
    {
        // Act & Assert
        var act = () => new Relationship(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), "   ");
        act.Should().Throw<DomainException>()
            .WithMessage("*RelationshipTypeObrigatorio*");
    }

    [Fact]
    public void Constructor_TypeWithWhitespace_ShouldTrim()
    {
        // Arrange
        var relationshipId = Guid.NewGuid();
        var sourceContactId = Guid.NewGuid();
        var targetContactId = Guid.NewGuid();

        // Act
        var relationship = new Relationship(relationshipId, sourceContactId, targetContactId, "  colleague  ");

        // Assert
        relationship.Type.Should().Be("colleague");
    }

    [Fact]
    public void Constructor_DescriptionWithWhitespace_ShouldTrim()
    {
        // Arrange
        var relationshipId = Guid.NewGuid();
        var sourceContactId = Guid.NewGuid();
        var targetContactId = Guid.NewGuid();

        // Act
        var relationship = new Relationship(relationshipId, sourceContactId, targetContactId, "colleague", "  description  ");

        // Assert
        relationship.Description.Should().Be("description");
    }

    [Fact]
    public void UpdateStrength_ValidStrength_ShouldUpdate()
    {
        // Arrange
        var relationship = CreateRelationship();
        var strength = 0.75f;

        // Act
        relationship.UpdateStrength(strength);

        // Assert
        relationship.Strength.Should().Be(strength);
    }

    [Fact]
    public void UpdateStrength_Zero_ShouldUpdate()
    {
        // Arrange
        var relationship = CreateRelationship();

        // Act
        relationship.UpdateStrength(0.0f);

        // Assert
        relationship.Strength.Should().Be(0.0f);
    }

    [Fact]
    public void UpdateStrength_One_ShouldUpdate()
    {
        // Arrange
        var relationship = CreateRelationship();

        // Act
        relationship.UpdateStrength(1.0f);

        // Assert
        relationship.Strength.Should().Be(1.0f);
    }

    [Fact]
    public void UpdateStrength_Negative_ShouldThrow()
    {
        // Arrange
        var relationship = CreateRelationship();

        // Act & Assert
        var act = () => relationship.UpdateStrength(-0.1f);
        act.Should().Throw<DomainException>()
            .WithMessage("*StrengthDeveEstarEntreZeroEUm*");
    }

    [Fact]
    public void UpdateStrength_GreaterThanOne_ShouldThrow()
    {
        // Arrange
        var relationship = CreateRelationship();

        // Act & Assert
        var act = () => relationship.UpdateStrength(1.1f);
        act.Should().Throw<DomainException>()
            .WithMessage("*StrengthDeveEstarEntreZeroEUm*");
    }

    [Fact]
    public void Confirm_ShouldSetIsConfirmedToTrue()
    {
        // Arrange
        var relationship = CreateRelationship();

        // Act
        relationship.Confirm();

        // Assert
        relationship.IsConfirmed.Should().BeTrue();
    }

    [Fact]
    public void UpdateDescription_ValidDescription_ShouldUpdate()
    {
        // Arrange
        var relationship = CreateRelationship();
        var newDescription = "New description";

        // Act
        relationship.UpdateDescription(newDescription);

        // Assert
        relationship.Description.Should().Be(newDescription);
    }

    [Fact]
    public void UpdateDescription_Null_ShouldSetToNull()
    {
        // Arrange
        var relationship = CreateRelationship();
        relationship.UpdateDescription("Some description");

        // Act
        relationship.UpdateDescription(null);

        // Assert
        relationship.Description.Should().BeNull();
    }

    [Fact]
    public void UpdateDescription_WithWhitespace_ShouldTrim()
    {
        // Arrange
        var relationship = CreateRelationship();

        // Act
        relationship.UpdateDescription("  description  ");

        // Assert
        relationship.Description.Should().Be("description");
    }

    private Relationship CreateRelationship()
    {
        return new Relationship(
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            "colleague",
            "Test relationship");
    }
}



