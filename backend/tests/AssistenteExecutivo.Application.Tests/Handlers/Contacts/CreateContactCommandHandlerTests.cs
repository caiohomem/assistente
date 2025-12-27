using AssistenteExecutivo.Application.Commands.Contacts;
using AssistenteExecutivo.Application.DTOs;
using AssistenteExecutivo.Application.Queries.Contacts;
using AssistenteExecutivo.Application.Tests.Helpers;
using AssistenteExecutivo.Domain.Exceptions;
using FluentAssertions;

namespace AssistenteExecutivo.Application.Tests.Handlers.Contacts;

public class CreateContactCommandHandlerTests : HandlerTestBase
{
    [Fact]
    public async Task Handle_ValidCommand_ShouldCreateContact()
    {
        // Arrange
        var ownerUserId = Guid.NewGuid();
        var command = new CreateContactCommand
        {
            OwnerUserId = ownerUserId,
            FirstName = "João",
            LastName = "Silva",
            JobTitle = "Desenvolvedor",
            Company = "Tech Corp",
            Street = "Rua das Flores, 123",
            City = "São Paulo",
            State = "SP",
            ZipCode = "01234-567",
            Country = "Brasil"
        };

        // Act
        var contactId = await SendAsync(command);

        // Assert
        contactId.Should().NotBeEmpty();

        // Verify contact was saved
        var query = new GetContactByIdQuery
        {
            ContactId = contactId,
            OwnerUserId = ownerUserId
        };
        var contact = await SendAsync(query);

        contact.Should().NotBeNull();
        contact!.FirstName.Should().Be("João");
        contact.LastName.Should().Be("Silva");
        contact.JobTitle.Should().Be("Desenvolvedor");
        contact.Company.Should().Be("Tech Corp");
        contact.Address.Should().NotBeNull();
        contact.Address!.City.Should().Be("São Paulo");
        contact.Address.State.Should().Be("SP");
    }

    [Fact]
    public async Task Handle_MinimalCommand_ShouldCreateContact()
    {
        // Arrange
        var ownerUserId = Guid.NewGuid();
        var command = new CreateContactCommand
        {
            OwnerUserId = ownerUserId,
            FirstName = "Maria"
        };

        // Act
        var contactId = await SendAsync(command);

        // Assert
        contactId.Should().NotBeEmpty();

        var query = new GetContactByIdQuery
        {
            ContactId = contactId,
            OwnerUserId = ownerUserId
        };
        var contact = await SendAsync(query);

        contact.Should().NotBeNull();
        contact!.FirstName.Should().Be("Maria");
        contact.LastName.Should().BeNull();
    }

    [Fact]
    public async Task Handle_EmptyOwnerUserId_ShouldThrowException()
    {
        // Arrange
        var command = new CreateContactCommand
        {
            OwnerUserId = Guid.Empty,
            FirstName = "João"
        };

        // Act & Assert
        var act = async () => await SendAsync(command);
        await act.Should().ThrowAsync<DomainException>()
            .WithMessage("*OwnerUserIdObrigatorio*");
    }
}





