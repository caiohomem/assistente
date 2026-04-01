using AssistenteExecutivo.Application.DTOs;
using AssistenteExecutivo.Application.Interfaces;
using AssistenteExecutivo.Application.Queries.Milestones;
using AssistenteExecutivo.Domain.Entities;
using AssistenteExecutivo.Domain.Exceptions;
using MediatR;

namespace AssistenteExecutivo.Application.Handlers.Milestones;

public class GetMilestoneByIdQueryHandler : IRequestHandler<GetMilestoneByIdQuery, MilestoneDto?>
{
    private readonly IMilestoneRepository _milestoneRepository;
    private readonly ICommissionAgreementRepository _agreementRepository;

    public GetMilestoneByIdQueryHandler(
        IMilestoneRepository milestoneRepository,
        ICommissionAgreementRepository agreementRepository)
    {
        _milestoneRepository = milestoneRepository;
        _agreementRepository = agreementRepository;
    }

    public async Task<MilestoneDto?> Handle(GetMilestoneByIdQuery request, CancellationToken cancellationToken)
    {
        if (request.MilestoneId == Guid.Empty)
            throw new DomainException("Domain:MilestoneIdObrigatorio");

        var milestone = await _milestoneRepository.GetByIdAsync(request.MilestoneId, cancellationToken);
        if (milestone == null)
            return null;

        var agreement = await _agreementRepository.GetByIdAsync(milestone.AgreementId, cancellationToken)
            ?? throw new DomainException("Domain:AcordoNaoEncontrado");

        EnsureOwner(agreement, request.RequestingUserId);

        return MilestoneMapper.Map(milestone);
    }

    private static void EnsureOwner(CommissionAgreement agreement, Guid requestingUserId)
    {
        if (requestingUserId == Guid.Empty || agreement.OwnerUserId != requestingUserId)
            throw new DomainException("Domain:UsuarioNaoAutorizado");
    }
}
