using AssistenteExecutivo.Application.Commands.Contacts;
using AssistenteExecutivo.Application.DTOs;
using AssistenteExecutivo.Application.Queries.Contacts;
using AssistenteExecutivo.Application.Tests.Helpers;
using AssistenteExecutivo.Domain.Exceptions;
using FluentAssertions;

namespace AssistenteExecutivo.Application.Tests.Handlers.Contacts;

public class UpdateContactCommandHandlerTests : HandlerTestBase
{
    [Fact]
    public async Task Handle_ValidUpdate_ShouldUpdateContact()
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
        await SendAsync(new AddContactEmailCommand
        {
            ContactId = contactId,
            OwnerUserId = ownerUserId,
            Email = "joao@example.com"
        });

        // Act
        var updateCommand = new UpdateContactCommand
        {
            ContactId = contactId,
            OwnerUserId = ownerUserId,
            FirstName = "João Pedro",
            LastName = "Silva Santos",
            JobTitle = "Senior Developer",
            Company = "New Company"
        };
        await SendAsync(updateCommand);

        // Assert
        var query = new GetContactByIdQuery
        {
            ContactId = contactId,
            OwnerUserId = ownerUserId
        };
        var contact = await SendAsync(query);

        contact.Should().NotBeNull();
        contact!.FirstName.Should().Be("João Pedro");
        contact.LastName.Should().Be("Silva Santos");
        contact.JobTitle.Should().Be("Senior Developer");
        contact.Company.Should().Be("New Company");
    }

    [Fact]
    public async Task Handle_UpdateAddress_ShouldUpdateAddress()
    {
        // Arrange
        var ownerUserId = Guid.NewGuid();
        var createCommand = new CreateContactCommand
        {
            OwnerUserId = ownerUserId,
            FirstName = "João"
        };
        var contactId = await SendAsync(createCommand);
        await SendAsync(new AddContactEmailCommand
        {
            ContactId = contactId,
            OwnerUserId = ownerUserId,
            Email = "joao@example.com"
        });

        // Act
        var updateCommand = new UpdateContactCommand
        {
            ContactId = contactId,
            OwnerUserId = ownerUserId,
            Street = "Avenida Paulista, 1000",
            City = "São Paulo",
            State = "SP",
            ZipCode = "01310-100",
            Country = "Brasil"
        };
        await SendAsync(updateCommand);

        // Assert
        var query = new GetContactByIdQuery
        {
            ContactId = contactId,
            OwnerUserId = ownerUserId
        };
        var contact = await SendAsync(query);

        contact.Should().NotBeNull();
        contact!.Address.Should().NotBeNull();
        contact.Address!.Street.Should().Be("Avenida Paulista, 1000");
        contact.Address.City.Should().Be("São Paulo");
        contact.Address.State.Should().Be("SP");
    }

    [Fact]
    public async Task Handle_NonExistentContact_ShouldThrowException()
    {
        // Arrange
        var ownerUserId = Guid.NewGuid();
        var updateCommand = new UpdateContactCommand
        {
            ContactId = Guid.NewGuid(),
            OwnerUserId = ownerUserId,
            FirstName = "João"
        };

        // Act & Assert
        var act = async () => await SendAsync(updateCommand);
        await act.Should().ThrowAsync<DomainException>()
            .WithMessage("*ContactNaoEncontrado*");
    }

    [Fact]
    public async Task Handle_EmptyOwnerUserId_ShouldThrowException()
    {
        // Arrange
        var updateCommand = new UpdateContactCommand
        {
            ContactId = Guid.NewGuid(),
            OwnerUserId = Guid.Empty,
            FirstName = "João"
        };

        // Act & Assert
        var act = async () => await SendAsync(updateCommand);
        await act.Should().ThrowAsync<DomainException>()
            .WithMessage("*OwnerUserIdObrigatorio*");
    }
}



