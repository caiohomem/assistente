using AssistenteExecutivo.Application.Interfaces;
using AssistenteExecutivo.Application.Queries.Commission;
using AssistenteExecutivo.Domain.Exceptions;
using AssistenteExecutivo.Domain.Interfaces;
using MediatR;
using System.Linq;

namespace AssistenteExecutivo.Application.Handlers.Commission;

public class GetAgreementAcceptanceStatusQueryHandler : IRequestHandler<GetAgreementAcceptanceStatusQuery, AgreementAcceptanceStatusDto>
{
    private const int DefaultMaxDays = 7;
    private readonly ICommissionAgreementRepository _agreementRepository;
    private readonly IClock _clock;

    public GetAgreementAcceptanceStatusQueryHandler(
        ICommissionAgreementRepository agreementRepository,
        IClock clock)
    {
        _agreementRepository = agreementRepository;
        _clock = clock;
    }

    public async Task<AgreementAcceptanceStatusDto> Handle(GetAgreementAcceptanceStatusQuery request, CancellationToken cancellationToken)
    {
        if (request.AgreementId == Guid.Empty)
            throw new DomainException("Domain:AgreementIdObrigatorio");
        if (request.OwnerUserId == Guid.Empty)
            throw new DomainException("Domain:UsuarioSolicitanteObrigatorio");

        var agreement = await _agreementRepository.GetByIdAsync(request.AgreementId, cancellationToken)
            ?? throw new DomainException("Domain:AcordoNaoEncontrado");

        if (agreement.OwnerUserId != request.OwnerUserId)
            throw new DomainException("Domain:UsuarioNaoAutorizado");

        var total = agreement.Parties.Count;
        var accepted = agreement.Parties.Count(p => p.HasAccepted);
        var pending = total - accepted;

        var maxDays = request.MaxDays.GetValueOrDefault(DefaultMaxDays);
        var startedAt = agreement.ActivatedAt ?? agreement.UpdatedAt;
        var daysElapsed = (int)Math.Floor((_clock.UtcNow - startedAt).TotalDays);
        if (daysElapsed < 0) daysElapsed = 0;

        var allAccepted = total > 0 && accepted == total;
        var isExpired = daysElapsed >= maxDays;

        if (agreement.Status == Domain.Enums.AgreementStatus.Active ||
            agreement.Status == Domain.Enums.AgreementStatus.Completed)
        {
            allAccepted = true;
            isExpired = false;
        }

        return new AgreementAcceptanceStatusDto
        {
            AgreementId = agreement.AgreementId,
            TotalParties = total,
            AcceptedParties = accepted,
            PendingParties = pending,
            DaysElapsed = daysElapsed,
            MaxDays = maxDays,
            AllAccepted = allAccepted,
            IsExpired = isExpired,
            Status = agreement.Status.ToString()
        };
    }
}
