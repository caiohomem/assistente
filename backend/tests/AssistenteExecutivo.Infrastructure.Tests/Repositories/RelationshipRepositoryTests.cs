using AssistenteExecutivo.Application.Interfaces;
using AssistenteExecutivo.Domain.Entities;
using AssistenteExecutivo.Domain.ValueObjects;
using AssistenteExecutivo.Infrastructure.Repositories;
using FluentAssertions;

namespace AssistenteExecutivo.Infrastructure.Tests.Repositories;

public class RelationshipRepositoryTests : RepositoryTestBase
{
    private readonly IRelationshipRepository _repository;

    public RelationshipRepositoryTests()
    {
        _repository = new RelationshipRepository(Context);
    }

    [Fact]
    public async Task GetByIdAsync_ExistingRelationship_ShouldReturnRelationship()
    {
        // Arrange
        var ownerUserId = Guid.NewGuid();
        var contact1 = new Contact(Guid.NewGuid(), ownerUserId, PersonName.Create("João", "Silva"), Clock);
        var contact2 = new Contact(Guid.NewGuid(), ownerUserId, PersonName.Create("Maria", "Santos"), Clock);
        var relationship = new Relationship(
            Guid.NewGuid(),
            contact1.ContactId,
            contact2.ContactId,
            "Colleague");

        await Context.Contacts.AddRangeAsync(contact1, contact2);
        await Context.Relationships.AddAsync(relationship);
        await SaveChangesAsync();

        // Act
        var result = await _repository.GetByIdAsync(relationship.RelationshipId);

        // Assert
        result.Should().NotBeNull();
        result!.RelationshipId.Should().Be(relationship.RelationshipId);
        result.SourceContactId.Should().Be(contact1.ContactId);
        result.TargetContactId.Should().Be(contact2.ContactId);
        result.Type.Should().Be("Colleague");
    }

    [Fact]
    public async Task GetByIdAsync_NonExistentRelationship_ShouldReturnNull()
    {
        // Arrange
        var relationshipId = Guid.NewGuid();

        // Act
        var result = await _repository.GetByIdAsync(relationshipId);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task GetByContactIdAsync_ShouldReturnRelationshipsForContact()
    {
        // Arrange
        var ownerUserId = Guid.NewGuid();
        var contact1 = new Contact(Guid.NewGuid(), ownerUserId, PersonName.Create("João", "Silva"), Clock);
        var contact2 = new Contact(Guid.NewGuid(), ownerUserId, PersonName.Create("Maria", "Santos"), Clock);
        var contact3 = new Contact(Guid.NewGuid(), ownerUserId, PersonName.Create("Pedro", "Costa"), Clock);

        var relationship1 = new Relationship(
            Guid.NewGuid(),
            contact1.ContactId,
            contact2.ContactId,
            "Colleague");
        var relationship2 = new Relationship(
            Guid.NewGuid(),
            contact3.ContactId,
            contact1.ContactId,
            "Friend");

        await Context.Contacts.AddRangeAsync(contact1, contact2, contact3);
        await Context.Relationships.AddRangeAsync(relationship1, relationship2);
        await SaveChangesAsync();

        // Act
        var result = await _repository.GetByContactIdAsync(contact1.ContactId, ownerUserId);

        // Assert
        result.Should().HaveCount(2);
        result.Should().Contain(r => r.RelationshipId == relationship1.RelationshipId);
        result.Should().Contain(r => r.RelationshipId == relationship2.RelationshipId);
    }

    [Fact]
    public async Task GetByContactIdAsync_ContactNotOwned_ShouldReturnEmpty()
    {
        // Arrange
        var ownerUserId1 = Guid.NewGuid();
        var ownerUserId2 = Guid.NewGuid();
        var contact1 = new Contact(Guid.NewGuid(), ownerUserId1, PersonName.Create("João", "Silva"), Clock);
        var contact2 = new Contact(Guid.NewGuid(), ownerUserId2, PersonName.Create("Maria", "Santos"), Clock);
        var relationship = new Relationship(
            Guid.NewGuid(),
            contact1.ContactId,
            contact2.ContactId,
            "Colleague");

        await Context.Contacts.AddRangeAsync(contact1, contact2);
        await Context.Relationships.AddAsync(relationship);
        await SaveChangesAsync();

        // Act
        var result = await _repository.GetByContactIdAsync(contact1.ContactId, ownerUserId2);

        // Assert
        result.Should().BeEmpty();
    }

    [Fact]
    public async Task GetBySourceAndTargetAsync_ExistingRelationship_ShouldReturnRelationship()
    {
        // Arrange
        var ownerUserId = Guid.NewGuid();
        var contact1 = new Contact(Guid.NewGuid(), ownerUserId, PersonName.Create("João", "Silva"), Clock);
        var contact2 = new Contact(Guid.NewGuid(), ownerUserId, PersonName.Create("Maria", "Santos"), Clock);
        var relationship = new Relationship(
            Guid.NewGuid(),
            contact1.ContactId,
            contact2.ContactId,
            "Colleague");

        await Context.Contacts.AddRangeAsync(contact1, contact2);
        await Context.Relationships.AddAsync(relationship);
        await SaveChangesAsync();

        // Act
        var result = await _repository.GetBySourceAndTargetAsync(contact1.ContactId, contact2.ContactId);

        // Assert
        result.Should().NotBeNull();
        result!.RelationshipId.Should().Be(relationship.RelationshipId);
    }

    [Fact]
    public async Task GetBySourceAndTargetAsync_NonExistentRelationship_ShouldReturnNull()
    {
        // Arrange
        var contact1Id = Guid.NewGuid();
        var contact2Id = Guid.NewGuid();

        // Act
        var result = await _repository.GetBySourceAndTargetAsync(contact1Id, contact2Id);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task AddAsync_ShouldAddRelationship()
    {
        // Arrange
        var ownerUserId = Guid.NewGuid();
        var contact1 = new Contact(Guid.NewGuid(), ownerUserId, PersonName.Create("João", "Silva"), Clock);
        var contact2 = new Contact(Guid.NewGuid(), ownerUserId, PersonName.Create("Maria", "Santos"), Clock);
        await Context.Contacts.AddRangeAsync(contact1, contact2);
        await SaveChangesAsync();

        var relationship = new Relationship(
            Guid.NewGuid(),
            contact1.ContactId,
            contact2.ContactId,
            "Colleague");

        // Act
        await _repository.AddAsync(relationship);
        await SaveChangesAsync();

        // Assert
        var result = await Context.Relationships.FindAsync(relationship.RelationshipId);
        result.Should().NotBeNull();
        result!.Type.Should().Be("Colleague");
    }

    [Fact]
    public async Task UpdateAsync_ShouldUpdateRelationship()
    {
        // Arrange
        var ownerUserId = Guid.NewGuid();
        var contact1 = new Contact(Guid.NewGuid(), ownerUserId, PersonName.Create("João", "Silva"), Clock);
        var contact2 = new Contact(Guid.NewGuid(), ownerUserId, PersonName.Create("Maria", "Santos"), Clock);
        var relationship = new Relationship(
            Guid.NewGuid(),
            contact1.ContactId,
            contact2.ContactId,
            "Colleague");

        await Context.Contacts.AddRangeAsync(contact1, contact2);
        await Context.Relationships.AddAsync(relationship);
        await SaveChangesAsync();

        relationship.UpdateDescription("Updated description");

        // Act
        await _repository.UpdateAsync(relationship);
        await SaveChangesAsync();

        // Assert
        var result = await Context.Relationships.FindAsync(relationship.RelationshipId);
        result!.Description.Should().Be("Updated description");
    }

    [Fact]
    public async Task DeleteAsync_ShouldDeleteRelationship()
    {
        // Arrange
        var ownerUserId = Guid.NewGuid();
        var contact1 = new Contact(Guid.NewGuid(), ownerUserId, PersonName.Create("João", "Silva"), Clock);
        var contact2 = new Contact(Guid.NewGuid(), ownerUserId, PersonName.Create("Maria", "Santos"), Clock);
        var relationship = new Relationship(
            Guid.NewGuid(),
            contact1.ContactId,
            contact2.ContactId,
            "Colleague");

        await Context.Contacts.AddRangeAsync(contact1, contact2);
        await Context.Relationships.AddAsync(relationship);
        await SaveChangesAsync();

        // Act
        await _repository.DeleteAsync(relationship);
        await SaveChangesAsync();

        // Assert
        var result = await Context.Relationships.FindAsync(relationship.RelationshipId);
        result.Should().BeNull();
    }
}

