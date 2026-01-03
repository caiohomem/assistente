using AssistenteExecutivo.Application.DTOs;
using AssistenteExecutivo.Domain.Enums;
using MediatR;

namespace AssistenteExecutivo.Application.Queries.Commission;

public class ListAgreementsQuery : IRequest<List<CommissionAgreementDto>>
{
    public Guid OwnerUserId { get; set; }
    public AgreementStatus? Status { get; set; }
}
