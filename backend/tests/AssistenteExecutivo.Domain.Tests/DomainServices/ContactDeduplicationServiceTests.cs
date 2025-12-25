using AssistenteExecutivo.Domain.DomainServices;
using AssistenteExecutivo.Domain.Entities;
using AssistenteExecutivo.Domain.Exceptions;
using AssistenteExecutivo.Domain.Interfaces;
using AssistenteExecutivo.Domain.ValueObjects;
using FluentAssertions;

namespace AssistenteExecutivo.Domain.Tests.DomainServices;

public class ContactDeduplicationServiceTests
{
    private readonly IClock _clock;
    private readonly ContactDeduplicationService _service;

    public ContactDeduplicationServiceTests()
    {
        _clock = new TestClock();
        _service = new ContactDeduplicationService();
    }

    [Fact]
    public void Decide_SameEmail_ShouldReturnMerge()
    {
        // Arrange
        var existingContact = CreateContactWithEmail("joao@example.com");
        var newExtract = new OcrExtract(
            name: "João Silva",
            email: "joao@example.com");

        // Act
        var decision = _service.Decide(existingContact, newExtract);

        // Assert
        decision.Action.Should().Be(DeduplicationAction.Merge);
        decision.ExistingContactId.Should().Be(existingContact.ContactId);
        decision.Reason.Should().Be("EmailExato");
    }

    [Fact]
    public void Decide_SamePhone_ShouldReturnMerge()
    {
        // Arrange
        var existingContact = CreateContactWithPhone("11987654321");
        var newExtract = new OcrExtract(
            name: "João Silva",
            phone: "11987654321");

        // Act
        var decision = _service.Decide(existingContact, newExtract);

        // Assert
        decision.Action.Should().Be(DeduplicationAction.Merge);
        decision.Reason.Should().Be("TelefoneExato");
    }

    [Fact]
    public void Decide_SimilarNameAndCompany_ShouldReturnMerge()
    {
        // Arrange
        var existingContact = CreateContactWithName("João Silva");
        var newExtract = new OcrExtract(
            name: "Joao Silva",
            company: "Empresa XYZ");

        // Act
        var decision = _service.Decide(existingContact, newExtract);

        // Assert
        decision.Action.Should().Be(DeduplicationAction.Merge);
        decision.Reason.Should().Be("NomeSimilarEmesmaEmpresa");
    }

    [Fact]
    public void Decide_DifferentData_ShouldReturnCreate()
    {
        // Arrange
        var existingContact = CreateContactWithEmail("joao@example.com");
        var newExtract = new OcrExtract(
            name: "Maria Santos",
            email: "maria@example.com");

        // Act
        var decision = _service.Decide(existingContact, newExtract);

        // Assert
        decision.Action.Should().Be(DeduplicationAction.Create);
        decision.ExistingContactId.Should().BeNull();
        decision.Reason.Should().Be("NaoDuplicado");
    }

    [Fact]
    public void Decide_VerySimilarName_ShouldReturnMerge()
    {
        // Arrange
        var existingContact = CreateContactWithName("João Silva");
        var newExtract = new OcrExtract(
            name: "João Silva");

        // Act
        var decision = _service.Decide(existingContact, newExtract);

        // Assert
        decision.Action.Should().Be(DeduplicationAction.Merge);
        decision.Reason.Should().Be("NomeMuitoSimilar");
    }

    [Fact]
    public void Decide_NullExistingContact_ShouldThrow()
    {
        // Arrange
        var newExtract = new OcrExtract(name: "João Silva");

        // Act & Assert
        var act = () => _service.Decide(null!, newExtract);
        act.Should().Throw<DomainException>()
            .WithMessage("*ExistingContactObrigatorio*");
    }

    [Fact]
    public void Decide_NullExtract_ShouldThrow()
    {
        // Arrange
        var existingContact = CreateContactWithEmail("joao@example.com");

        // Act & Assert
        var act = () => _service.Decide(existingContact, null!);
        act.Should().Throw<DomainException>()
            .WithMessage("*OcrExtractObrigatorio*");
    }

    private Contact CreateContactWithEmail(string email)
    {
        var contactId = Guid.NewGuid();
        var ownerUserId = Guid.NewGuid();
        var name = PersonName.Create("João", "Silva");
        var contact = new Contact(contactId, ownerUserId, name, _clock);
        contact.AddEmail(EmailAddress.Create(email));
        return contact;
    }

    private Contact CreateContactWithPhone(string phone)
    {
        var contactId = Guid.NewGuid();
        var ownerUserId = Guid.NewGuid();
        var name = PersonName.Create("João", "Silva");
        var contact = new Contact(contactId, ownerUserId, name, _clock);
        contact.AddPhone(PhoneNumber.Create(phone));
        return contact;
    }

    private Contact CreateContactWithName(string fullName)
    {
        var parts = fullName.Split(' ');
        var contactId = Guid.NewGuid();
        var ownerUserId = Guid.NewGuid();
        var name = PersonName.Create(parts[0], parts.Length > 1 ? parts[1] : null);
        return new Contact(contactId, ownerUserId, name, _clock);
    }

    private class TestClock : IClock
    {
        public DateTime UtcNow => DateTime.UtcNow;
    }
}

