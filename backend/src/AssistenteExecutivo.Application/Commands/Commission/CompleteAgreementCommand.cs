using MediatR;

namespace AssistenteExecutivo.Application.Commands.Commission;

public class CompleteAgreementCommand : IRequest<Unit>
{
    public Guid AgreementId { get; set; }
    public Guid RequestedBy { get; set; }
}
