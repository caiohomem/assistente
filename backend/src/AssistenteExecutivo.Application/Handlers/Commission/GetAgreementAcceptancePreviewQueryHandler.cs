using AssistenteExecutivo.Application.Interfaces;
using AssistenteExecutivo.Application.Queries.Commission;
using MediatR;

namespace AssistenteExecutivo.Application.Handlers.Commission;

public sealed class GetAgreementAcceptancePreviewQueryHandler
    : IRequestHandler<GetAgreementAcceptancePreviewQuery, AgreementAcceptancePreviewDto?>
{
    private readonly IAgreementAcceptanceTokenService _tokenService;
    private readonly ICommissionAgreementRepository _agreementRepository;

    public GetAgreementAcceptancePreviewQueryHandler(
        IAgreementAcceptanceTokenService tokenService,
        ICommissionAgreementRepository agreementRepository)
    {
        _tokenService = tokenService;
        _agreementRepository = agreementRepository;
    }

    public async Task<AgreementAcceptancePreviewDto?> Handle(
        GetAgreementAcceptancePreviewQuery request,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.Token))
        {
            return null;
        }

        if (!_tokenService.TryValidateToken(request.Token, out var payload))
        {
            return null;
        }

        var agreement = await _agreementRepository.GetByIdAsync(payload.AgreementId, cancellationToken);
        if (agreement == null)
        {
            return null;
        }

        var party = agreement.Parties.FirstOrDefault(p => p.PartyId == payload.PartyId);
        if (party == null)
        {
            return null;
        }

        return new AgreementAcceptancePreviewDto
        {
            AgreementId = agreement.AgreementId,
            PartyId = party.PartyId,
            OwnerUserId = payload.OwnerUserId,
            MaxDays = payload.MaxDays,
            AgreementTitle = agreement.Title,
            PartyName = party.PartyName,
            ExpiresAt = payload.ExpiresAt.UtcDateTime,
            HasAccepted = party.HasAccepted
        };
    }
}
