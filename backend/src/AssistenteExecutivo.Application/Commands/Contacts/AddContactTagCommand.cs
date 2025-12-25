using MediatR;

namespace AssistenteExecutivo.Application.Commands.Contacts;

public class AddContactTagCommand : IRequest<Unit>
{
    public Guid ContactId { get; set; }
    public Guid OwnerUserId { get; set; }
    public string Tag { get; set; } = string.Empty;
}


