using AssistenteExecutivo.Domain.DomainEvents;
using AssistenteExecutivo.Domain.Entities;
using AssistenteExecutivo.Domain.Enums;
using AssistenteExecutivo.Domain.Exceptions;
using AssistenteExecutivo.Domain.Interfaces;
using FluentAssertions;

namespace AssistenteExecutivo.Domain.Tests.Entities;

public class TemplateTests
{
    private readonly IClock _clock;

    public TemplateTests()
    {
        _clock = new TestClock();
    }

    [Fact]
    public void Constructor_ValidData_ShouldCreate()
    {
        // Arrange
        var templateId = Guid.NewGuid();
        var ownerUserId = Guid.NewGuid();
        var name = "Email Template";
        var type = TemplateType.Email;
        var body = "Template body";

        // Act
        var template = new Template(templateId, ownerUserId, name, type, body, _clock);

        // Assert
        template.TemplateId.Should().Be(templateId);
        template.OwnerUserId.Should().Be(ownerUserId);
        template.Name.Should().Be(name);
        template.Type.Should().Be(type);
        template.Body.Should().Be(body);
        template.PlaceholdersSchema.Should().BeNull();
        template.Active.Should().BeTrue();
    }

    [Fact]
    public void Constructor_EmptyTemplateId_ShouldThrow()
    {
        // Act & Assert
        var act = () => new Template(Guid.Empty, Guid.NewGuid(), "Template", TemplateType.Email, "Body", _clock);
        act.Should().Throw<DomainException>()
            .WithMessage("*TemplateIdObrigatorio*");
    }

    [Fact]
    public void Constructor_EmptyOwnerUserId_ShouldThrow()
    {
        // Act & Assert
        var act = () => new Template(Guid.NewGuid(), Guid.Empty, "Template", TemplateType.Email, "Body", _clock);
        act.Should().Throw<DomainException>()
            .WithMessage("*OwnerUserIdObrigatorio*");
    }

    [Fact]
    public void Constructor_EmptyName_ShouldThrow()
    {
        // Act & Assert
        var act = () => new Template(Guid.NewGuid(), Guid.NewGuid(), "", TemplateType.Email, "Body", _clock);
        act.Should().Throw<DomainException>()
            .WithMessage("*TemplateNameObrigatorio*");
    }

    [Fact]
    public void Constructor_EmptyBody_ShouldThrow()
    {
        // Act & Assert
        var act = () => new Template(Guid.NewGuid(), Guid.NewGuid(), "Template", TemplateType.Email, "", _clock);
        act.Should().Throw<DomainException>()
            .WithMessage("*TemplateBodyObrigatorio*");
    }

    [Fact]
    public void Constructor_NullClock_ShouldThrow()
    {
        // Act & Assert
        var act = () => new Template(Guid.NewGuid(), Guid.NewGuid(), "Template", TemplateType.Email, "Body", null!);
        act.Should().Throw<DomainException>()
            .WithMessage("*ClockObrigatorio*");
    }

    [Fact]
    public void Constructor_NameWithWhitespace_ShouldTrim()
    {
        // Arrange
        var templateId = Guid.NewGuid();
        var ownerUserId = Guid.NewGuid();

        // Act
        var template = new Template(templateId, ownerUserId, "  Template  ", TemplateType.Email, "Body", _clock);

        // Assert
        template.Name.Should().Be("Template");
    }

    [Fact]
    public void Create_ValidData_ShouldCreateAndEmitEvent()
    {
        // Arrange
        var templateId = Guid.NewGuid();
        var ownerUserId = Guid.NewGuid();
        var name = "Email Template";
        var type = TemplateType.Email;
        var body = "Template body";

        // Act
        var template = Template.Create(templateId, ownerUserId, name, type, body, _clock);

        // Assert
        template.TemplateId.Should().Be(templateId);
        template.DomainEvents.Should().ContainSingle()
            .Which.Should().BeOfType<TemplateCreated>();
    }

    [Fact]
    public void UpdateName_ValidName_ShouldUpdate()
    {
        // Arrange
        var template = CreateTemplate();
        var newName = "New Template Name";

        // Act
        template.UpdateName(newName, _clock);

        // Assert
        template.Name.Should().Be(newName);
        template.UpdatedAt.Should().BeCloseTo(_clock.UtcNow, TimeSpan.FromSeconds(1));
    }

    [Fact]
    public void UpdateName_EmptyName_ShouldThrow()
    {
        // Arrange
        var template = CreateTemplate();

        // Act & Assert
        var act = () => template.UpdateName("", _clock);
        act.Should().Throw<DomainException>()
            .WithMessage("*TemplateNameObrigatorio*");
    }

    [Fact]
    public void UpdateName_WithWhitespace_ShouldTrim()
    {
        // Arrange
        var template = CreateTemplate();

        // Act
        template.UpdateName("  New Name  ", _clock);

        // Assert
        template.Name.Should().Be("New Name");
    }

    [Fact]
    public void UpdateBody_ValidBody_ShouldUpdate()
    {
        // Arrange
        var template = CreateTemplate();
        var newBody = "New template body";

        // Act
        template.UpdateBody(newBody, _clock);

        // Assert
        template.Body.Should().Be(newBody);
        template.UpdatedAt.Should().BeCloseTo(_clock.UtcNow, TimeSpan.FromSeconds(1));
    }

    [Fact]
    public void UpdateBody_EmptyBody_ShouldThrow()
    {
        // Arrange
        var template = CreateTemplate();

        // Act & Assert
        var act = () => template.UpdateBody("", _clock);
        act.Should().Throw<DomainException>()
            .WithMessage("*TemplateBodyObrigatorio*");
    }

    [Fact]
    public void UpdatePlaceholdersSchema_ValidSchema_ShouldUpdate()
    {
        // Arrange
        var template = CreateTemplate();
        var schema = "{\"type\":\"object\"}";

        // Act
        template.UpdatePlaceholdersSchema(schema, _clock);

        // Assert
        template.PlaceholdersSchema.Should().Be(schema);
        template.UpdatedAt.Should().BeCloseTo(_clock.UtcNow, TimeSpan.FromSeconds(1));
    }

    [Fact]
    public void UpdatePlaceholdersSchema_Null_ShouldSetToNull()
    {
        // Arrange
        var template = CreateTemplate();
        template.UpdatePlaceholdersSchema("{\"type\":\"object\"}", _clock);

        // Act
        template.UpdatePlaceholdersSchema(null, _clock);

        // Assert
        template.PlaceholdersSchema.Should().BeNull();
    }

    [Fact]
    public void Activate_WhenInactive_ShouldActivate()
    {
        // Arrange
        var template = CreateTemplate();
        template.Deactivate(_clock);

        // Act
        template.Activate(_clock);

        // Assert
        template.Active.Should().BeTrue();
        template.UpdatedAt.Should().BeCloseTo(_clock.UtcNow, TimeSpan.FromSeconds(1));
    }

    [Fact]
    public void Activate_WhenActive_ShouldNotUpdate()
    {
        // Arrange
        var template = CreateTemplate();
        var originalUpdatedAt = template.UpdatedAt;
        Thread.Sleep(10); // Small delay to ensure time difference

        // Act
        template.Activate(_clock);

        // Assert
        template.Active.Should().BeTrue();
        // UpdatedAt should not change if already active
        template.UpdatedAt.Should().BeCloseTo(originalUpdatedAt, TimeSpan.FromSeconds(1));
    }

    [Fact]
    public void Deactivate_WhenActive_ShouldDeactivate()
    {
        // Arrange
        var template = CreateTemplate();

        // Act
        template.Deactivate(_clock);

        // Assert
        template.Active.Should().BeFalse();
        template.UpdatedAt.Should().BeCloseTo(_clock.UtcNow, TimeSpan.FromSeconds(1));
    }

    [Fact]
    public void Deactivate_WhenInactive_ShouldNotUpdate()
    {
        // Arrange
        var template = CreateTemplate();
        template.Deactivate(_clock);
        var originalUpdatedAt = template.UpdatedAt;
        Thread.Sleep(10); // Small delay to ensure time difference

        // Act
        template.Deactivate(_clock);

        // Assert
        template.Active.Should().BeFalse();
        // UpdatedAt should not change if already inactive
        template.UpdatedAt.Should().BeCloseTo(originalUpdatedAt, TimeSpan.FromSeconds(1));
    }

    private Template CreateTemplate()
    {
        return new Template(
            Guid.NewGuid(),
            Guid.NewGuid(),
            "Test Template",
            TemplateType.Email,
            "Template body",
            _clock);
    }

    private class TestClock : IClock
    {
        public DateTime UtcNow => DateTime.UtcNow;
    }
}



