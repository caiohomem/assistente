using MediatR;

namespace AssistenteExecutivo.Application.Commands.Commission;

public class CancelAgreementCommand : IRequest<Unit>
{
    public Guid AgreementId { get; set; }
    public Guid RequestedBy { get; set; }
    public string Reason { get; set; } = string.Empty;
}
