using MediatR;

namespace AssistenteExecutivo.Application.Commands.Contacts;

public class DeleteContactCommand : IRequest<Unit>
{
    public Guid ContactId { get; set; }
    public Guid OwnerUserId { get; set; }
}










