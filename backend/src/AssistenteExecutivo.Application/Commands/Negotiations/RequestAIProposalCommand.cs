using MediatR;

namespace AssistenteExecutivo.Application.Commands.Negotiations;

public class RequestAIProposalCommand : IRequest<Guid>
{
    public Guid SessionId { get; set; }
    public Guid RequestedBy { get; set; }
    public string? AdditionalInstructions { get; set; }
}
