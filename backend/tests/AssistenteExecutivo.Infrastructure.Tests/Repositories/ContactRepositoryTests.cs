using AssistenteExecutivo.Application.Interfaces;
using AssistenteExecutivo.Domain.Entities;
using AssistenteExecutivo.Domain.ValueObjects;
using AssistenteExecutivo.Infrastructure.Repositories;
using FluentAssertions;

namespace AssistenteExecutivo.Infrastructure.Tests.Repositories;

public class ContactRepositoryTests : RepositoryTestBase
{
    private readonly IContactRepository _repository;

    public ContactRepositoryTests()
    {
        _repository = new ContactRepository(Context);
    }

    [Fact]
    public async Task GetByIdAsync_ExistingContact_ShouldReturnContact()
    {
        // Arrange
        var ownerUserId = Guid.NewGuid();
        var contactId = Guid.NewGuid();
        var contact = new Contact(contactId, ownerUserId, PersonName.Create("João", "Silva"), Clock);
        contact.AddEmail(EmailAddress.Create("joao@example.com"));

        await Context.Contacts.AddAsync(contact);
        await SaveChangesAsync();

        // Act
        var result = await _repository.GetByIdAsync(contactId, ownerUserId);

        // Assert
        result.Should().NotBeNull();
        result!.ContactId.Should().Be(contactId);
        result.OwnerUserId.Should().Be(ownerUserId);
        result.Name.FullName.Should().Be("João Silva");
        result.Emails.Should().ContainSingle(e => e.Value == "joao@example.com");
    }

    [Fact]
    public async Task GetByIdAsync_NonExistentContact_ShouldReturnNull()
    {
        // Arrange
        var ownerUserId = Guid.NewGuid();
        var contactId = Guid.NewGuid();

        // Act
        var result = await _repository.GetByIdAsync(contactId, ownerUserId);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task GetByIdAsync_DifferentOwner_ShouldReturnNull()
    {
        // Arrange
        var ownerUserId1 = Guid.NewGuid();
        var ownerUserId2 = Guid.NewGuid();
        var contactId = Guid.NewGuid();
        var contact = new Contact(contactId, ownerUserId1, PersonName.Create("João", "Silva"), Clock);

        await Context.Contacts.AddAsync(contact);
        await SaveChangesAsync();

        // Act
        var result = await _repository.GetByIdAsync(contactId, ownerUserId2);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task GetByIdAsync_DeletedContact_ShouldReturnNull()
    {
        // Arrange
        var ownerUserId = Guid.NewGuid();
        var contactId = Guid.NewGuid();
        var contact = new Contact(contactId, ownerUserId, PersonName.Create("João", "Silva"), Clock);
        contact.Delete();

        await Context.Contacts.AddAsync(contact);
        await SaveChangesAsync();

        // Act
        var result = await _repository.GetByIdAsync(contactId, ownerUserId);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task GetAllAsync_ShouldReturnAllContactsForOwner()
    {
        // Arrange
        var ownerUserId1 = Guid.NewGuid();
        var ownerUserId2 = Guid.NewGuid();

        var contact1 = new Contact(Guid.NewGuid(), ownerUserId1, PersonName.Create("João", "Silva"), Clock);
        var contact2 = new Contact(Guid.NewGuid(), ownerUserId1, PersonName.Create("Maria", "Santos"), Clock);
        var contact3 = new Contact(Guid.NewGuid(), ownerUserId2, PersonName.Create("Pedro", "Costa"), Clock);

        await Context.Contacts.AddRangeAsync(contact1, contact2, contact3);
        await SaveChangesAsync();

        // Act
        var result = await _repository.GetAllAsync(ownerUserId1);

        // Assert
        result.Should().HaveCount(2);
        result.Should().Contain(c => c.ContactId == contact1.ContactId);
        result.Should().Contain(c => c.ContactId == contact2.ContactId);
        result.Should().NotContain(c => c.ContactId == contact3.ContactId);
    }

    [Fact]
    public async Task GetAllAsync_IncludeDeleted_ShouldReturnDeletedContacts()
    {
        // Arrange
        var ownerUserId = Guid.NewGuid();
        var contact1 = new Contact(Guid.NewGuid(), ownerUserId, PersonName.Create("João", "Silva"), Clock);
        var contact2 = new Contact(Guid.NewGuid(), ownerUserId, PersonName.Create("Maria", "Santos"), Clock);
        contact2.Delete();

        await Context.Contacts.AddRangeAsync(contact1, contact2);
        await SaveChangesAsync();

        // Act
        var result = await _repository.GetAllAsync(ownerUserId, includeDeleted: true);

        // Assert
        result.Should().HaveCount(2);
    }

    [Fact]
    public async Task GetByEmailAsync_ExistingEmail_ShouldReturnContact()
    {
        // Arrange
        var ownerUserId = Guid.NewGuid();
        var contact = new Contact(Guid.NewGuid(), ownerUserId, PersonName.Create("João", "Silva"), Clock);
        contact.AddEmail(EmailAddress.Create("joao@example.com"));

        await Context.Contacts.AddAsync(contact);
        await SaveChangesAsync();

        // Act
        var result = await _repository.GetByEmailAsync("joao@example.com", ownerUserId);

        // Assert
        result.Should().NotBeNull();
        result!.ContactId.Should().Be(contact.ContactId);
    }

    [Fact]
    public async Task GetByEmailAsync_CaseInsensitive_ShouldReturnContact()
    {
        // Arrange
        var ownerUserId = Guid.NewGuid();
        var contact = new Contact(Guid.NewGuid(), ownerUserId, PersonName.Create("João", "Silva"), Clock);
        contact.AddEmail(EmailAddress.Create("Joao@Example.com"));

        await Context.Contacts.AddAsync(contact);
        await SaveChangesAsync();

        // Act
        var result = await _repository.GetByEmailAsync("joao@example.com", ownerUserId);

        // Assert
        result.Should().NotBeNull();
    }

    [Fact]
    public async Task GetByPhoneAsync_ExistingPhone_ShouldReturnContact()
    {
        // Arrange
        var ownerUserId = Guid.NewGuid();
        var contact = new Contact(Guid.NewGuid(), ownerUserId, PersonName.Create("João", "Silva"), Clock);
        contact.AddPhone(PhoneNumber.Create("11987654321"));

        await Context.Contacts.AddAsync(contact);
        await SaveChangesAsync();

        // Act
        var result = await _repository.GetByPhoneAsync("(11) 98765-4321", ownerUserId);

        // Assert
        result.Should().NotBeNull();
        result!.ContactId.Should().Be(contact.ContactId);
    }

    [Fact]
    public async Task AddAsync_ShouldAddContact()
    {
        // Arrange
        var contact = new Contact(Guid.NewGuid(), Guid.NewGuid(), PersonName.Create("João", "Silva"), Clock);

        // Act
        await _repository.AddAsync(contact);
        await SaveChangesAsync();

        // Assert
        var result = await Context.Contacts.FindAsync(contact.ContactId);
        result.Should().NotBeNull();
        result!.Name.FullName.Should().Be("João Silva");
    }

    [Fact]
    public async Task UpdateAsync_ShouldUpdateContact()
    {
        // Arrange
        var contact = new Contact(Guid.NewGuid(), Guid.NewGuid(), PersonName.Create("João", "Silva"), Clock);
        await Context.Contacts.AddAsync(contact);
        await SaveChangesAsync();

        contact.UpdateDetails(
            PersonName.Create("João", "Silva Santos"),
            contact.JobTitle,
            contact.Company,
            contact.Address);

        // Act
        await _repository.UpdateAsync(contact);
        await SaveChangesAsync();

        // Assert
        var result = await Context.Contacts.FindAsync(contact.ContactId);
        result!.Name.FullName.Should().Be("João Silva Santos");
    }

    [Fact]
    public async Task DeleteAsync_ShouldSoftDeleteContact()
    {
        // Arrange
        var contact = new Contact(Guid.NewGuid(), Guid.NewGuid(), PersonName.Create("João", "Silva"), Clock);
        await Context.Contacts.AddAsync(contact);
        await SaveChangesAsync();

        // Act
        await _repository.DeleteAsync(contact);
        await SaveChangesAsync();

        // Assert
        var result = await Context.Contacts.FindAsync(contact.ContactId);
        result!.IsDeleted.Should().BeTrue();
    }

    [Fact]
    public async Task ExistsAsync_ExistingContact_ShouldReturnTrue()
    {
        // Arrange
        var ownerUserId = Guid.NewGuid();
        var contactId = Guid.NewGuid();
        var contact = new Contact(contactId, ownerUserId, PersonName.Create("João", "Silva"), Clock);
        await Context.Contacts.AddAsync(contact);
        await SaveChangesAsync();

        // Act
        var result = await _repository.ExistsAsync(contactId, ownerUserId);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public async Task ExistsAsync_NonExistentContact_ShouldReturnFalse()
    {
        // Arrange
        var ownerUserId = Guid.NewGuid();
        var contactId = Guid.NewGuid();

        // Act
        var result = await _repository.ExistsAsync(contactId, ownerUserId);

        // Assert
        result.Should().BeFalse();
    }
}

