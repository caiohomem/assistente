using System.Linq;
using AssistenteExecutivo.Application.DTOs;
using AssistenteExecutivo.Application.Interfaces;
using AssistenteExecutivo.Application.Queries.Milestones;
using AssistenteExecutivo.Domain.Entities;
using AssistenteExecutivo.Domain.Exceptions;
using MediatR;

namespace AssistenteExecutivo.Application.Handlers.Milestones;

public class ListMilestonesByAgreementQueryHandler : IRequestHandler<ListMilestonesByAgreementQuery, List<MilestoneDto>>
{
    private readonly IMilestoneRepository _milestoneRepository;
    private readonly ICommissionAgreementRepository _agreementRepository;

    public ListMilestonesByAgreementQueryHandler(
        IMilestoneRepository milestoneRepository,
        ICommissionAgreementRepository agreementRepository)
    {
        _milestoneRepository = milestoneRepository;
        _agreementRepository = agreementRepository;
    }

    public async Task<List<MilestoneDto>> Handle(ListMilestonesByAgreementQuery request, CancellationToken cancellationToken)
    {
        if (request.AgreementId == Guid.Empty)
            throw new DomainException("Domain:AgreementIdObrigatorio");

        var agreement = await _agreementRepository.GetByIdAsync(request.AgreementId, cancellationToken)
            ?? throw new DomainException("Domain:AcordoNaoEncontrado");

        EnsureOwner(agreement, request.RequestingUserId);

        var milestones = await _milestoneRepository.ListByAgreementAsync(request.AgreementId, cancellationToken);
        return milestones.Select(MilestoneMapper.Map).ToList();
    }

    private static void EnsureOwner(CommissionAgreement agreement, Guid requestingUserId)
    {
        if (requestingUserId == Guid.Empty || agreement.OwnerUserId != requestingUserId)
            throw new DomainException("Domain:UsuarioNaoAutorizado");
    }
}
