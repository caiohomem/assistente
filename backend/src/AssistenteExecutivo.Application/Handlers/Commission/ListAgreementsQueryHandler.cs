using System.Linq;
using AssistenteExecutivo.Application.DTOs;
using AssistenteExecutivo.Application.Interfaces;
using AssistenteExecutivo.Application.Queries.Commission;
using MediatR;

namespace AssistenteExecutivo.Application.Handlers.Commission;

public class ListAgreementsQueryHandler : IRequestHandler<ListAgreementsQuery, List<CommissionAgreementDto>>
{
    private readonly ICommissionAgreementRepository _agreementRepository;

    public ListAgreementsQueryHandler(ICommissionAgreementRepository agreementRepository)
    {
        _agreementRepository = agreementRepository;
    }

    public async Task<List<CommissionAgreementDto>> Handle(ListAgreementsQuery request, CancellationToken cancellationToken)
    {
        var agreements = await _agreementRepository.ListByOwnerAsync(request.OwnerUserId, cancellationToken);

        if (request.Status.HasValue)
        {
            agreements = agreements
                .Where(a => a.Status == request.Status.Value)
                .ToList();
        }

        return agreements.Select(CommissionAgreementMapper.Map).ToList();
    }
}
