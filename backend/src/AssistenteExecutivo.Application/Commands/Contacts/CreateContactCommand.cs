using MediatR;

namespace AssistenteExecutivo.Application.Commands.Contacts;

public class CreateContactCommand : IRequest<Guid>
{
    public Guid OwnerUserId { get; set; }
    public string FirstName { get; set; } = string.Empty;
    public string? LastName { get; set; }
    public string? JobTitle { get; set; }
    public string? Company { get; set; }
    public string? Street { get; set; }
    public string? City { get; set; }
    public string? State { get; set; }
    public string? ZipCode { get; set; }
    public string? Country { get; set; }
}






