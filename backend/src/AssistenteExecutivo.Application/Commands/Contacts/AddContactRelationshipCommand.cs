using MediatR;

namespace AssistenteExecutivo.Application.Commands.Contacts;

public class AddContactRelationshipCommand : IRequest<Unit>
{
    public Guid ContactId { get; set; }
    public Guid OwnerUserId { get; set; }
    public Guid TargetContactId { get; set; }
    public string Type { get; set; } = string.Empty;
    public string? Description { get; set; }
}



