using AssistenteExecutivo.Application.DTOs;
using MediatR;

namespace AssistenteExecutivo.Application.Queries.Commission;

public class GetAgreementByIdQuery : IRequest<CommissionAgreementDto?>
{
    public Guid AgreementId { get; set; }
    public Guid RequestingUserId { get; set; }
}
