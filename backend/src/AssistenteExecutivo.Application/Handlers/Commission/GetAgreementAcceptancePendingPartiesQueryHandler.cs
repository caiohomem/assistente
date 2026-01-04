using AssistenteExecutivo.Application.Interfaces;
using AssistenteExecutivo.Application.Queries.Commission;
using AssistenteExecutivo.Domain.Exceptions;
using AssistenteExecutivo.Domain.Interfaces;
using AssistenteExecutivo.Domain.Notifications;
using MediatR;
using System.Linq;

namespace AssistenteExecutivo.Application.Handlers.Commission;

public class GetAgreementAcceptancePendingPartiesQueryHandler
    : IRequestHandler<GetAgreementAcceptancePendingPartiesQuery, AgreementAcceptancePendingPartiesDto>
{
    private const int DefaultMaxDays = 7;
    private readonly ICommissionAgreementRepository _agreementRepository;
    private readonly IAgreementAcceptanceTokenService _tokenService;
    private readonly IEmailTemplateRepository _templateRepository;
    private readonly IClock _clock;

    public GetAgreementAcceptancePendingPartiesQueryHandler(
        ICommissionAgreementRepository agreementRepository,
        IAgreementAcceptanceTokenService tokenService,
        IEmailTemplateRepository templateRepository,
        IClock clock)
    {
        _agreementRepository = agreementRepository;
        _tokenService = tokenService;
        _templateRepository = templateRepository;
        _clock = clock;
    }

    public async Task<AgreementAcceptancePendingPartiesDto> Handle(
        GetAgreementAcceptancePendingPartiesQuery request,
        CancellationToken cancellationToken)
    {
        if (request.AgreementId == Guid.Empty)
            throw new DomainException("Domain:AgreementIdObrigatorio");
        if (request.OwnerUserId == Guid.Empty)
            throw new DomainException("Domain:UsuarioSolicitanteObrigatorio");
        if (string.IsNullOrWhiteSpace(request.ApiBaseUrl))
            throw new ArgumentException("ApiBaseUrl é obrigatório.", nameof(request.ApiBaseUrl));

        var agreement = await _agreementRepository.GetByIdAsync(request.AgreementId, cancellationToken)
            ?? throw new DomainException("Domain:AcordoNaoEncontrado");

        if (agreement.OwnerUserId != request.OwnerUserId)
            throw new DomainException("Domain:UsuarioNaoAutorizado");

        var maxDays = request.MaxDays.GetValueOrDefault(DefaultMaxDays);
        var startedAt = agreement.ActivatedAt ?? agreement.UpdatedAt;
        var expiresAt = startedAt.AddDays(maxDays);

        EmailTemplate? template = null;
        if (!string.IsNullOrWhiteSpace(request.TemplateName))
        {
            template = await _templateRepository.GetByNameAsync(request.TemplateName!, cancellationToken);
            if (template == null || !template.IsActive)
            {
                throw new DomainException("Domain:EmailTemplateNaoEncontrado");
            }
        }

        var parties = agreement.Parties
            .Where(p => request.IncludeAccepted || !p.HasAccepted)
            .Select(p =>
            {
                var acceptUrl = string.IsNullOrWhiteSpace(p.Email)
                    ? null
                    : BuildAcceptUrl(request.ApiBaseUrl, agreement.AgreementId, p.PartyId, agreement.OwnerUserId, maxDays, expiresAt);

                var (subject, html) = ResolveEmailContent(template, agreement, p, acceptUrl, expiresAt);

                return new AgreementAcceptancePartyDto
                {
                    PartyId = p.PartyId,
                    PartyName = p.PartyName,
                    Email = p.Email,
                    SplitPercentage = p.SplitPercentage.Value,
                    Role = p.Role,
                    HasAccepted = p.HasAccepted,
                    AcceptedAt = p.AcceptedAt,
                    AcceptUrl = acceptUrl,
                    EmailSubject = subject,
                    EmailHtml = html
                };
            })
            .ToList();

        return new AgreementAcceptancePendingPartiesDto
        {
            AgreementId = agreement.AgreementId,
            Title = agreement.Title,
            Description = agreement.Description,
            Terms = agreement.Terms,
            TotalValue = agreement.TotalValue.Amount,
            Currency = agreement.TotalValue.Currency,
            ExpiresAt = expiresAt,
            MaxDays = maxDays,
            Parties = parties
        };
    }

    private string BuildAcceptUrl(string apiBaseUrl, Guid agreementId, Guid partyId, Guid ownerUserId, int maxDays, DateTime expiresAt)
    {
        var token = _tokenService.CreateToken(agreementId, partyId, ownerUserId, maxDays, new DateTimeOffset(expiresAt));
        return $"{apiBaseUrl.TrimEnd('/')}/acordos/aceite?token={Uri.EscapeDataString(token)}";
    }

    private static (string? Subject, string? Html) ResolveEmailContent(
        EmailTemplate? template,
        Domain.Entities.CommissionAgreement agreement,
        Domain.Entities.AgreementParty party,
        string? acceptUrl,
        DateTime expiresAt)
    {
        if (template == null || string.IsNullOrWhiteSpace(acceptUrl))
        {
            return (null, null);
        }

        var values = new Dictionary<string, object>
        {
            ["PartyName"] = party.PartyName,
            ["AgreementTitle"] = agreement.Title,
            ["TotalValue"] = agreement.TotalValue.Amount.ToString("N2"),
            ["Currency"] = agreement.TotalValue.Currency,
            ["AcceptUrl"] = acceptUrl,
            ["ExpiresAt"] = expiresAt.ToString("dd/MM/yyyy"),
            ["SplitPercentage"] = party.SplitPercentage.Value.ToString("N2"),
            ["Description"] = agreement.Description ?? string.Empty,
            ["Terms"] = agreement.Terms ?? string.Empty
        };

        var subject = template.ApplySubject(values);
        var html = template.ApplyTemplate(values);
        return (subject, html);
    }
}
