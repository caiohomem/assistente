using System.Linq;
using AssistenteExecutivo.Application.DTOs;
using AssistenteExecutivo.Application.Interfaces;
using AssistenteExecutivo.Application.Queries.Commission;
using AssistenteExecutivo.Domain.Entities;
using AssistenteExecutivo.Domain.Exceptions;
using MediatR;

namespace AssistenteExecutivo.Application.Handlers.Commission;

public class GetAgreementByIdQueryHandler : IRequestHandler<GetAgreementByIdQuery, CommissionAgreementDto?>
{
    private readonly ICommissionAgreementRepository _agreementRepository;

    public GetAgreementByIdQueryHandler(ICommissionAgreementRepository agreementRepository)
    {
        _agreementRepository = agreementRepository;
    }

    public async Task<CommissionAgreementDto?> Handle(GetAgreementByIdQuery request, CancellationToken cancellationToken)
    {
        if (request.AgreementId == Guid.Empty)
            throw new DomainException("Domain:AgreementIdObrigatorio");

        var agreement = await _agreementRepository.GetByIdAsync(request.AgreementId, cancellationToken);
        if (agreement == null)
            return null;

        EnsureOwner(agreement, request.RequestingUserId);

        return CommissionAgreementMapper.Map(agreement);
    }

    private static void EnsureOwner(CommissionAgreement agreement, Guid userId)
    {
        if (agreement.OwnerUserId != userId)
            throw new DomainException("Domain:UsuarioNaoAutorizado");
    }
}
