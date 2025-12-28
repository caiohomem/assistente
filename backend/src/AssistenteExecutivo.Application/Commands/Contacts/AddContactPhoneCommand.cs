using MediatR;

namespace AssistenteExecutivo.Application.Commands.Contacts;

public class AddContactPhoneCommand : IRequest<Unit>
{
    public Guid ContactId { get; set; }
    public Guid OwnerUserId { get; set; }
    public string Phone { get; set; } = string.Empty;
}










