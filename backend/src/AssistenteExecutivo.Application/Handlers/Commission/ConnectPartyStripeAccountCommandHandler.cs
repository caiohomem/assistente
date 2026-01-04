using AssistenteExecutivo.Application.Commands.Commission;
using AssistenteExecutivo.Application.Interfaces;
using AssistenteExecutivo.Domain.Entities;
using AssistenteExecutivo.Domain.Exceptions;
using AssistenteExecutivo.Domain.Interfaces;
using MediatR;
using System.Linq;

namespace AssistenteExecutivo.Application.Handlers.Commission;

public class ConnectPartyStripeAccountCommandHandler : IRequestHandler<ConnectPartyStripeAccountCommand, Unit>
{
    private readonly ICommissionAgreementRepository _agreementRepository;
    private readonly IPaymentGateway _paymentGateway;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IClock _clock;

    public ConnectPartyStripeAccountCommandHandler(
        ICommissionAgreementRepository agreementRepository,
        IPaymentGateway paymentGateway,
        IUnitOfWork unitOfWork,
        IClock clock)
    {
        _agreementRepository = agreementRepository;
        _paymentGateway = paymentGateway;
        _unitOfWork = unitOfWork;
        _clock = clock;
    }

    public async Task<Unit> Handle(ConnectPartyStripeAccountCommand request, CancellationToken cancellationToken)
    {
        if (request.AgreementId == Guid.Empty)
            throw new DomainException("Domain:AgreementIdObrigatorio");
        if (request.PartyId == Guid.Empty)
            throw new DomainException("Domain:PartyIdObrigatorio");
        if (string.IsNullOrWhiteSpace(request.AuthorizationCodeOrAccountId))
            throw new DomainException("Domain:CodigoAutorizacaoOuAccountIdObrigatorio");
        if (request.RequestedBy == Guid.Empty)
            throw new DomainException("Domain:UsuarioSolicitanteObrigatorio");

        var agreement = await _agreementRepository.GetByIdAsync(request.AgreementId, cancellationToken)
            ?? throw new DomainException("Domain:AcordoNaoEncontrado");

        EnsureOwner(agreement, request.RequestedBy);

        var party = agreement.Parties.FirstOrDefault(p => p.PartyId == request.PartyId)
            ?? throw new DomainException("Domain:ParteNaoEncontrada");

        // Aceitar tanto código OAuth quanto Account ID direto
        string accountId;
        if (request.AuthorizationCodeOrAccountId.StartsWith("acct_", StringComparison.OrdinalIgnoreCase))
        {
            accountId = request.AuthorizationCodeOrAccountId;
        }
        else
        {
            accountId = await _paymentGateway.ConnectAccountAsync(
                request.RequestedBy,
                request.AuthorizationCodeOrAccountId,
                cancellationToken);
        }

        agreement.ConnectPartyStripeAccount(request.PartyId, accountId, _clock);
        await _agreementRepository.UpdateAsync(agreement, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Unit.Value;
    }

    private static void EnsureOwner(CommissionAgreement agreement, Guid userId)
    {
        if (agreement.OwnerUserId != userId)
            throw new DomainException("Domain:ApenasDonoPodeModificarAcordo");
    }
}
