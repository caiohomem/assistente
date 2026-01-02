using AssistenteExecutivo.Domain.DomainEvents;
using AssistenteExecutivo.Domain.Entities;
using AssistenteExecutivo.Domain.Enums;
using AssistenteExecutivo.Domain.Exceptions;
using AssistenteExecutivo.Domain.Interfaces;
using FluentAssertions;

namespace AssistenteExecutivo.Domain.Tests.Entities;

public class DraftDocumentTests
{
    private readonly IClock _clock;

    public DraftDocumentTests()
    {
        _clock = new TestClock();
    }

    [Fact]
    public void Constructor_ValidData_ShouldCreate()
    {
        // Arrange
        var draftId = Guid.NewGuid();
        var ownerUserId = Guid.NewGuid();
        var documentType = DocumentType.Email;
        var content = "Email content";

        // Act
        var draft = new DraftDocument(draftId, ownerUserId, documentType, content, _clock);

        // Assert
        draft.DraftId.Should().Be(draftId);
        draft.OwnerUserId.Should().Be(ownerUserId);
        draft.DocumentType.Should().Be(documentType);
        draft.Content.Should().Be(content);
        draft.Status.Should().Be(DraftStatus.Draft);
        draft.ContactId.Should().BeNull();
        draft.CompanyId.Should().BeNull();
        draft.TemplateId.Should().BeNull();
        draft.LetterheadId.Should().BeNull();
    }

    [Fact]
    public void Constructor_EmptyDraftId_ShouldThrow()
    {
        // Act & Assert
        var act = () => new DraftDocument(Guid.Empty, Guid.NewGuid(), DocumentType.Email, "content", _clock);
        act.Should().Throw<DomainException>()
            .WithMessage("*DraftIdObrigatorio*");
    }

    [Fact]
    public void Constructor_EmptyOwnerUserId_ShouldThrow()
    {
        // Act & Assert
        var act = () => new DraftDocument(Guid.NewGuid(), Guid.Empty, DocumentType.Email, "content", _clock);
        act.Should().Throw<DomainException>()
            .WithMessage("*OwnerUserIdObrigatorio*");
    }

    [Fact]
    public void Constructor_EmptyContent_ShouldThrow()
    {
        // Act & Assert
        var act = () => new DraftDocument(Guid.NewGuid(), Guid.NewGuid(), DocumentType.Email, "", _clock);
        act.Should().Throw<DomainException>()
            .WithMessage("*DraftContentObrigatorio*");
    }

    [Fact]
    public void Constructor_NullClock_ShouldThrow()
    {
        // Act & Assert
        var act = () => new DraftDocument(Guid.NewGuid(), Guid.NewGuid(), DocumentType.Email, "content", null!);
        act.Should().Throw<DomainException>()
            .WithMessage("*ClockObrigatorio*");
    }

    [Fact]
    public void Create_ValidData_ShouldCreateAndEmitEvent()
    {
        // Arrange
        var draftId = Guid.NewGuid();
        var ownerUserId = Guid.NewGuid();
        var documentType = DocumentType.Email;
        var content = "Email content";

        // Act
        var draft = DraftDocument.Create(draftId, ownerUserId, documentType, content, _clock);

        // Assert
        draft.DraftId.Should().Be(draftId);
        draft.DomainEvents.Should().ContainSingle()
            .Which.Should().BeOfType<DraftCreated>();
    }

    [Fact]
    public void AssociateContact_ValidContactId_ShouldAssociate()
    {
        // Arrange
        var draft = CreateDraft();
        var contactId = Guid.NewGuid();

        // Act
        draft.AssociateContact(contactId, _clock);

        // Assert
        draft.ContactId.Should().Be(contactId);
        draft.UpdatedAt.Should().BeCloseTo(_clock.UtcNow, TimeSpan.FromSeconds(1));
    }

    [Fact]
    public void AssociateContact_EmptyContactId_ShouldThrow()
    {
        // Arrange
        var draft = CreateDraft();

        // Act & Assert
        var act = () => draft.AssociateContact(Guid.Empty, _clock);
        act.Should().Throw<DomainException>()
            .WithMessage("*ContactIdObrigatorio*");
    }

    [Fact]
    public void AssociateCompany_ValidCompanyId_ShouldAssociate()
    {
        // Arrange
        var draft = CreateDraft();
        var companyId = Guid.NewGuid();

        // Act
        draft.AssociateCompany(companyId, _clock);

        // Assert
        draft.CompanyId.Should().Be(companyId);
        draft.UpdatedAt.Should().BeCloseTo(_clock.UtcNow, TimeSpan.FromSeconds(1));
    }

    [Fact]
    public void AssociateCompany_EmptyCompanyId_ShouldThrow()
    {
        // Arrange
        var draft = CreateDraft();

        // Act & Assert
        var act = () => draft.AssociateCompany(Guid.Empty, _clock);
        act.Should().Throw<DomainException>()
            .WithMessage("*CompanyIdObrigatorio*");
    }

    [Fact]
    public void SetTemplate_ValidTemplateId_ShouldSet()
    {
        // Arrange
        var draft = CreateDraft();
        var templateId = Guid.NewGuid();

        // Act
        draft.SetTemplate(templateId, _clock);

        // Assert
        draft.TemplateId.Should().Be(templateId);
        draft.UpdatedAt.Should().BeCloseTo(_clock.UtcNow, TimeSpan.FromSeconds(1));
    }

    [Fact]
    public void SetTemplate_EmptyTemplateId_ShouldThrow()
    {
        // Arrange
        var draft = CreateDraft();

        // Act & Assert
        var act = () => draft.SetTemplate(Guid.Empty, _clock);
        act.Should().Throw<DomainException>()
            .WithMessage("*TemplateIdObrigatorio*");
    }

    [Fact]
    public void SetLetterhead_ValidLetterheadId_ShouldSet()
    {
        // Arrange
        var draft = CreateDraft();
        var letterheadId = Guid.NewGuid();

        // Act
        draft.SetLetterhead(letterheadId, _clock);

        // Assert
        draft.LetterheadId.Should().Be(letterheadId);
        draft.UpdatedAt.Should().BeCloseTo(_clock.UtcNow, TimeSpan.FromSeconds(1));
    }

    [Fact]
    public void SetLetterhead_EmptyLetterheadId_ShouldThrow()
    {
        // Arrange
        var draft = CreateDraft();

        // Act & Assert
        var act = () => draft.SetLetterhead(Guid.Empty, _clock);
        act.Should().Throw<DomainException>()
            .WithMessage("*LetterheadIdObrigatorio*");
    }

    [Fact]
    public void UpdateContent_ValidContent_ShouldUpdate()
    {
        // Arrange
        var draft = CreateDraft();
        var newContent = "New content";

        // Act
        draft.UpdateContent(newContent, _clock);

        // Assert
        draft.Content.Should().Be(newContent);
        draft.UpdatedAt.Should().BeCloseTo(_clock.UtcNow, TimeSpan.FromSeconds(1));
    }

    [Fact]
    public void UpdateContent_EmptyContent_ShouldThrow()
    {
        // Arrange
        var draft = CreateDraft();

        // Act & Assert
        var act = () => draft.UpdateContent("", _clock);
        act.Should().Throw<DomainException>()
            .WithMessage("*DraftContentObrigatorio*");
    }

    [Fact]
    public void UpdateContent_SentDraft_ShouldThrow()
    {
        // Arrange
        var draft = CreateDraft();
        draft.Approve(Guid.NewGuid(), _clock);
        draft.Send(Guid.NewGuid(), _clock);

        // Act & Assert
        var act = () => draft.UpdateContent("New content", _clock);
        act.Should().Throw<DomainException>()
            .WithMessage("*DraftEnviadoNaoPodeSerEditado*");
    }

    [Fact]
    public void Approve_DraftStatus_ShouldApprove()
    {
        // Arrange
        var draft = CreateDraft();
        draft.ClearDomainEvents();
        var approvedBy = Guid.NewGuid();

        // Act
        draft.Approve(approvedBy, _clock);

        // Assert
        draft.Status.Should().Be(DraftStatus.Approved);
        draft.UpdatedAt.Should().BeCloseTo(_clock.UtcNow, TimeSpan.FromSeconds(1));
        draft.DomainEvents.Should().ContainSingle()
            .Which.Should().BeOfType<DraftApproved>();
    }

    [Fact]
    public void Approve_NotDraftStatus_ShouldThrow()
    {
        // Arrange
        var draft = CreateDraft();
        draft.Approve(Guid.NewGuid(), _clock);

        // Act & Assert
        var act = () => draft.Approve(Guid.NewGuid(), _clock);
        act.Should().Throw<DomainException>()
            .WithMessage("*DraftSoPodeSerAprovadoSeEmRascunho*");
    }

    [Fact]
    public void Send_FromDraft_ShouldSend()
    {
        // Arrange
        var draft = CreateDraft();
        draft.ClearDomainEvents();
        var sentBy = Guid.NewGuid();

        // Act
        draft.Send(sentBy, _clock);

        // Assert
        draft.Status.Should().Be(DraftStatus.Sent);
        draft.UpdatedAt.Should().BeCloseTo(_clock.UtcNow, TimeSpan.FromSeconds(1));
        draft.DomainEvents.Should().ContainSingle()
            .Which.Should().BeOfType<DraftSent>();
    }

    [Fact]
    public void Send_FromApproved_ShouldSend()
    {
        // Arrange
        var draft = CreateDraft();
        draft.Approve(Guid.NewGuid(), _clock);
        draft.ClearDomainEvents();
        var sentBy = Guid.NewGuid();

        // Act
        draft.Send(sentBy, _clock);

        // Assert
        draft.Status.Should().Be(DraftStatus.Sent);
        draft.DomainEvents.Should().ContainSingle()
            .Which.Should().BeOfType<DraftSent>();
    }

    [Fact]
    public void Send_FromSent_ShouldThrow()
    {
        // Arrange
        var draft = CreateDraft();
        draft.Send(Guid.NewGuid(), _clock);

        // Act & Assert
        var act = () => draft.Send(Guid.NewGuid(), _clock);
        act.Should().Throw<DomainException>()
            .WithMessage("*DraftSoPodeSerEnviadoSeAprovadoOuEmRascunho*");
    }

    private DraftDocument CreateDraft()
    {
        return new DraftDocument(
            Guid.NewGuid(),
            Guid.NewGuid(),
            DocumentType.Email,
            "Test content",
            _clock);
    }

    private class TestClock : IClock
    {
        public DateTime UtcNow => DateTime.UtcNow;
    }
}





