using MediatR;

namespace AssistenteExecutivo.Application.Commands.Commission;

public class ActivateAgreementCommand : IRequest<Unit>
{
    public Guid AgreementId { get; set; }
    public Guid RequestedBy { get; set; }
}
