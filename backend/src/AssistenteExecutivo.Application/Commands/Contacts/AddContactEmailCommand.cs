using MediatR;

namespace AssistenteExecutivo.Application.Commands.Contacts;

public class AddContactEmailCommand : IRequest<Unit>
{
    public Guid ContactId { get; set; }
    public Guid OwnerUserId { get; set; }
    public string Email { get; set; } = string.Empty;
}












