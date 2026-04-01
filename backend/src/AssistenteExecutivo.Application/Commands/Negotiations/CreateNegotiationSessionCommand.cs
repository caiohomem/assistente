using MediatR;

namespace AssistenteExecutivo.Application.Commands.Negotiations;

public class CreateNegotiationSessionCommand : IRequest<Guid>
{
    public Guid SessionId { get; set; }
    public Guid OwnerUserId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? Context { get; set; }
}
