using AssistenteExecutivo.Application.Interfaces;
using AssistenteExecutivo.Domain.DomainEvents;
using AssistenteExecutivo.Domain.Notifications;
using MediatR;

namespace AssistenteExecutivo.Application.Handlers.Commission;

public sealed class AgreementActivatedNotificationHandler : INotificationHandler<AgreementActivated>
{
    private readonly ICommissionAgreementRepository _agreementRepository;
    private readonly IEmailService _emailService;

    public AgreementActivatedNotificationHandler(
        ICommissionAgreementRepository agreementRepository,
        IEmailService emailService)
    {
        _agreementRepository = agreementRepository;
        _emailService = emailService;
    }

    public async Task Handle(AgreementActivated notification, CancellationToken cancellationToken)
    {
        var agreement = await _agreementRepository.GetByIdAsync(notification.AgreementId, cancellationToken);
        if (agreement == null)
        {
            return;
        }

        foreach (var party in agreement.Parties)
        {
            if (string.IsNullOrWhiteSpace(party.Email))
            {
                continue;
            }

            var templateValues = new Dictionary<string, object>
            {
                ["PartyName"] = party.PartyName,
                ["AgreementTitle"] = agreement.Title,
                ["TotalValue"] = agreement.TotalValue.Amount.ToString("N2"),
                ["Currency"] = agreement.TotalValue.Currency,
                ["SplitPercentage"] = party.SplitPercentage.Value.ToString("N2"),
                ["ApprovedAt"] = notification.OccurredAt.ToString("dd/MM/yyyy")
            };

            await _emailService.SendEmailWithTemplateAsync(
                EmailTemplateType.AgreementApproved,
                party.Email,
                party.PartyName,
                templateValues,
                cancellationToken);
        }
    }
}
