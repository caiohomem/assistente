using AssistenteExecutivo.Application.Interfaces;
using AssistenteExecutivo.Domain.Entities;
using AssistenteExecutivo.Domain.ValueObjects;
using AssistenteExecutivo.Infrastructure.Repositories;
using FluentAssertions;

namespace AssistenteExecutivo.Infrastructure.Tests.Repositories;

public class NoteRepositoryTests : RepositoryTestBase
{
    private readonly INoteRepository _repository;

    public NoteRepositoryTests()
    {
        _repository = new NoteRepository(Context);
    }

    [Fact]
    public async Task GetByIdAsync_ExistingNote_ShouldReturnNote()
    {
        // Arrange
        var ownerUserId = Guid.NewGuid();
        var contact = new Contact(Guid.NewGuid(), ownerUserId, PersonName.Create("João", "Silva"), Clock);
        var note = Note.CreateTextNote(
            Guid.NewGuid(),
            contact.ContactId,
            ownerUserId,
            "Test note content",
            Clock);

        await Context.Contacts.AddAsync(contact);
        await Context.Notes.AddAsync(note);
        await SaveChangesAsync();

        // Act
        var result = await _repository.GetByIdAsync(note.NoteId);

        // Assert
        result.Should().NotBeNull();
        result!.NoteId.Should().Be(note.NoteId);
        result.RawContent.Should().Be("Test note content");
    }

    [Fact]
    public async Task GetByIdAsync_NonExistentNote_ShouldReturnNull()
    {
        // Arrange
        var noteId = Guid.NewGuid();

        // Act
        var result = await _repository.GetByIdAsync(noteId);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task GetByContactIdAsync_ShouldReturnNotesForContact()
    {
        // Arrange
        var ownerUserId = Guid.NewGuid();
        var contact1 = new Contact(Guid.NewGuid(), ownerUserId, PersonName.Create("João", "Silva"), Clock);
        var contact2 = new Contact(Guid.NewGuid(), ownerUserId, PersonName.Create("Maria", "Santos"), Clock);

        var note1 = Note.CreateTextNote(
            Guid.NewGuid(),
            contact1.ContactId,
            ownerUserId,
            "Note 1",
            Clock);
        var note2 = Note.CreateTextNote(
            Guid.NewGuid(),
            contact1.ContactId,
            ownerUserId,
            "Note 2",
            Clock);
        var note3 = Note.CreateTextNote(
            Guid.NewGuid(),
            contact2.ContactId,
            ownerUserId,
            "Note 3",
            Clock);

        await Context.Contacts.AddRangeAsync(contact1, contact2);
        await Context.Notes.AddRangeAsync(note1, note2, note3);
        await SaveChangesAsync();

        // Act
        var result = await _repository.GetByContactIdAsync(contact1.ContactId, ownerUserId);

        // Assert
        result.Should().HaveCount(2);
        result.Should().Contain(n => n.NoteId == note1.NoteId);
        result.Should().Contain(n => n.NoteId == note2.NoteId);
        result.Should().NotContain(n => n.NoteId == note3.NoteId);
    }

    [Fact]
    public async Task GetByContactIdAsync_ContactNotOwned_ShouldReturnEmpty()
    {
        // Arrange
        var ownerUserId1 = Guid.NewGuid();
        var ownerUserId2 = Guid.NewGuid();
        var contact = new Contact(Guid.NewGuid(), ownerUserId1, PersonName.Create("João", "Silva"), Clock);
        var note = Note.CreateTextNote(
            Guid.NewGuid(),
            contact.ContactId,
            ownerUserId1,
            "Test note",
            Clock);

        await Context.Contacts.AddAsync(contact);
        await Context.Notes.AddAsync(note);
        await SaveChangesAsync();

        // Act
        var result = await _repository.GetByContactIdAsync(contact.ContactId, ownerUserId2);

        // Assert
        result.Should().BeEmpty();
    }

    [Fact]
    public async Task GetByAuthorIdAsync_ShouldReturnNotesByAuthor()
    {
        // Arrange
        var ownerUserId = Guid.NewGuid();
        var authorId1 = Guid.NewGuid();
        var authorId2 = Guid.NewGuid();
        var contact = new Contact(Guid.NewGuid(), ownerUserId, PersonName.Create("João", "Silva"), Clock);

        var note1 = Note.CreateTextNote(
            Guid.NewGuid(),
            contact.ContactId,
            authorId1,
            "Note 1",
            Clock);
        var note2 = Note.CreateTextNote(
            Guid.NewGuid(),
            contact.ContactId,
            authorId1,
            "Note 2",
            Clock);
        var note3 = Note.CreateTextNote(
            Guid.NewGuid(),
            contact.ContactId,
            authorId2,
            "Note 3",
            Clock);

        await Context.Contacts.AddAsync(contact);
        await Context.Notes.AddRangeAsync(note1, note2, note3);
        await SaveChangesAsync();

        // Act
        var result = await _repository.GetByAuthorIdAsync(authorId1, ownerUserId);

        // Assert
        result.Should().HaveCount(2);
        result.Should().Contain(n => n.NoteId == note1.NoteId);
        result.Should().Contain(n => n.NoteId == note2.NoteId);
        result.Should().NotContain(n => n.NoteId == note3.NoteId);
    }

    [Fact]
    public async Task AddAsync_ShouldAddNote()
    {
        // Arrange
        var ownerUserId = Guid.NewGuid();
        var contact = new Contact(Guid.NewGuid(), ownerUserId, PersonName.Create("João", "Silva"), Clock);
        await Context.Contacts.AddAsync(contact);
        await SaveChangesAsync();

        var note = Note.CreateTextNote(
            Guid.NewGuid(),
            contact.ContactId,
            ownerUserId,
            "Test note",
            Clock);

        // Act
        await _repository.AddAsync(note);
        await SaveChangesAsync();

        // Assert
        var result = await Context.Notes.FindAsync(note.NoteId);
        result.Should().NotBeNull();
        result!.RawContent.Should().Be("Test note");
    }

    [Fact]
    public async Task UpdateAsync_ShouldUpdateNote()
    {
        // Arrange
        var ownerUserId = Guid.NewGuid();
        var contact = new Contact(Guid.NewGuid(), ownerUserId, PersonName.Create("João", "Silva"), Clock);
        var note = Note.CreateTextNote(
            Guid.NewGuid(),
            contact.ContactId,
            ownerUserId,
            "Original content",
            Clock);

        await Context.Contacts.AddAsync(contact);
        await Context.Notes.AddAsync(note);
        await SaveChangesAsync();

        note.UpdateRawContent("Updated content", Clock);

        // Act
        await _repository.UpdateAsync(note);
        await SaveChangesAsync();

        // Assert
        var result = await Context.Notes.FindAsync(note.NoteId);
        result!.RawContent.Should().Be("Updated content");
    }

    [Fact]
    public async Task DeleteAsync_ShouldDeleteNote()
    {
        // Arrange
        var ownerUserId = Guid.NewGuid();
        var contact = new Contact(Guid.NewGuid(), ownerUserId, PersonName.Create("João", "Silva"), Clock);
        var note = Note.CreateTextNote(
            Guid.NewGuid(),
            contact.ContactId,
            ownerUserId,
            "Test note",
            Clock);

        await Context.Contacts.AddAsync(contact);
        await Context.Notes.AddAsync(note);
        await SaveChangesAsync();

        // Act
        await _repository.DeleteAsync(note);
        await SaveChangesAsync();

        // Assert
        var result = await Context.Notes.FindAsync(note.NoteId);
        result.Should().BeNull();
    }
}

