using MediatR;

namespace AssistenteExecutivo.Application.Commands.Negotiations;

public class GenerateAgreementFromNegotiationCommand : IRequest<Guid>
{
    public Guid SessionId { get; set; }
    public Guid AgreementId { get; set; }
    public Guid OwnerUserId { get; set; }
}
