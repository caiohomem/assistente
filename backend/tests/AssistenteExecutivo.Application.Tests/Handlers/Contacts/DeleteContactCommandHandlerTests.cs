using AssistenteExecutivo.Application.Commands.Contacts;
using AssistenteExecutivo.Application.Queries.Contacts;
using AssistenteExecutivo.Application.Tests.Helpers;
using AssistenteExecutivo.Domain.Exceptions;
using FluentAssertions;

namespace AssistenteExecutivo.Application.Tests.Handlers.Contacts;

public class DeleteContactCommandHandlerTests : HandlerTestBase
{
    [Fact]
    public async Task Handle_ValidDelete_ShouldDeleteContact()
    {
        // Arrange
        var ownerUserId = Guid.NewGuid();
        var createCommand = new CreateContactCommand
        {
            OwnerUserId = ownerUserId,
            FirstName = "João",
            LastName = "Silva"
        };
        var contactId = await SendAsync(createCommand);

        // Verify contact exists
        var query = new GetContactByIdQuery
        {
            ContactId = contactId,
            OwnerUserId = ownerUserId
        };
        var contactBefore = await SendAsync(query);
        contactBefore.Should().NotBeNull();

        // Act
        var deleteCommand = new DeleteContactCommand
        {
            ContactId = contactId,
            OwnerUserId = ownerUserId
        };
        await SendAsync(deleteCommand);

        // Assert
        var contactAfter = await SendAsync(query);
        contactAfter.Should().BeNull();
    }

    [Fact]
    public async Task Handle_NonExistentContact_ShouldThrowException()
    {
        // Arrange
        var ownerUserId = Guid.NewGuid();
        var deleteCommand = new DeleteContactCommand
        {
            ContactId = Guid.NewGuid(),
            OwnerUserId = ownerUserId
        };

        // Act & Assert
        var act = async () => await SendAsync(deleteCommand);
        await act.Should().ThrowAsync<DomainException>()
            .WithMessage("*ContactNaoEncontrado*");
    }

    [Fact]
    public async Task Handle_EmptyOwnerUserId_ShouldThrowException()
    {
        // Arrange
        var deleteCommand = new DeleteContactCommand
        {
            ContactId = Guid.NewGuid(),
            OwnerUserId = Guid.Empty
        };

        // Act & Assert
        var act = async () => await SendAsync(deleteCommand);
        await act.Should().ThrowAsync<DomainException>()
            .WithMessage("*OwnerUserIdObrigatorio*");
    }
}





