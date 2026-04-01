using System.Linq;
using AssistenteExecutivo.Application.Commands.Escrow;
using AssistenteExecutivo.Application.Interfaces;
using AssistenteExecutivo.Domain.DomainServices;
using AssistenteExecutivo.Domain.Enums;
using AssistenteExecutivo.Domain.Exceptions;
using AssistenteExecutivo.Domain.Interfaces;
using MediatR;

namespace AssistenteExecutivo.Application.Handlers.Escrow;

public class ExecutePayoutCommandHandler : IRequestHandler<ExecutePayoutCommand, Unit>
{
    private readonly IEscrowAccountRepository _escrowAccountRepository;
    private readonly ICommissionAgreementRepository _agreementRepository;
    private readonly IPaymentGateway _paymentGateway;
    private readonly EscrowPayoutDomainService _domainService;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IClock _clock;
    private readonly IPublisher _publisher;

    public ExecutePayoutCommandHandler(
        IEscrowAccountRepository escrowAccountRepository,
        ICommissionAgreementRepository agreementRepository,
        IPaymentGateway paymentGateway,
        EscrowPayoutDomainService domainService,
        IUnitOfWork unitOfWork,
        IClock clock,
        IPublisher publisher)
    {
        _escrowAccountRepository = escrowAccountRepository;
        _agreementRepository = agreementRepository;
        _paymentGateway = paymentGateway;
        _domainService = domainService;
        _unitOfWork = unitOfWork;
        _clock = clock;
        _publisher = publisher;
    }

    public async Task<Unit> Handle(ExecutePayoutCommand request, CancellationToken cancellationToken)
    {
        if (request.PerformedBy == Guid.Empty)
            throw new DomainException("Domain:UsuarioObrigatorio");

        var account = await _escrowAccountRepository.GetByIdAsync(request.EscrowAccountId, cancellationToken)
            ?? throw new DomainException("Domain:ContaEscrowNaoEncontrada");

        EnsureOwner(account, request.PerformedBy);

        if (string.IsNullOrWhiteSpace(account.StripeConnectedAccountId))
            throw new DomainException("Domain:ContaStripeNaoConectada");

        var transaction = account.Transactions.FirstOrDefault(t => t.TransactionId == request.TransactionId)
            ?? throw new DomainException("Domain:TransacaoNaoEncontrada");

        if (transaction.Status != EscrowTransactionStatus.Approved &&
            transaction.Status != EscrowTransactionStatus.Pending)
        {
            throw new DomainException("Domain:TransacaoNaoPodeSerExecutada");
        }

        var agreement = await _agreementRepository.GetByIdAsync(account.AgreementId, cancellationToken)
            ?? throw new DomainException("Domain:AcordoNaoEncontrado");

        // Verificar se há partes com Stripe conectado para split payout
        var partiesWithStripe = agreement.Parties
            .Where(p => !string.IsNullOrWhiteSpace(p.StripeAccountId))
            .ToList();

        string transferIds;

        if (partiesWithStripe.Count > 0)
        {
            // Nova lógica: Split payout para múltiplas partes
            _domainService.EnsureAllPartiesHaveStripeAccounts(agreement);

            var splits = _domainService.CalculatePayoutSplits(agreement, transaction.Amount);

            var transfers = splits.Select(s => (
                destinationAccountId: s.StripeAccountId,
                amount: s.Amount.Amount,
                currency: s.Amount.Currency,
                description: $"Split payout - {agreement.Title}"
            )).ToList();

            var splitPayoutResult = await _paymentGateway.ExecuteSplitPayoutAsync(
                account.StripeConnectedAccountId,
                transfers,
                cancellationToken);

            var normalizedStatus = splitPayoutResult.Status?.ToLowerInvariant() ?? string.Empty;
            if (!normalizedStatus.Contains("succeeded"))
                throw new DomainException("Domain:PayoutNaoExecutado");

            transferIds = string.Join(",", splitPayoutResult.TransferIds);
        }
        else
        {
            // Lógica legada: Payout para conta única do escrow
            var payoutResult = await _paymentGateway.ExecuteEscrowPayoutAsync(
                account.EscrowAccountId,
                transaction.TransactionId,
                transaction.Amount,
                account.StripeConnectedAccountId,
                cancellationToken);

            var normalizedStatus = payoutResult.Status?.ToLowerInvariant() ?? string.Empty;
            if (!(normalizedStatus.Contains("succeeded") || normalizedStatus.Contains("paid") || normalizedStatus.Contains("completed")))
                throw new DomainException("Domain:PayoutNaoExecutado");

            transferIds = string.IsNullOrWhiteSpace(request.StripeTransferId)
                ? payoutResult.TransferId
                : request.StripeTransferId;
        }

        account.MarkPayoutExecuted(request.TransactionId, transferIds, _clock);

        await _escrowAccountRepository.UpdateAsync(account, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        await PublishDomainEventsAsync(account, cancellationToken);
        account.ClearDomainEvents();

        return Unit.Value;
    }

    private static void EnsureOwner(Domain.Entities.EscrowAccount account, Guid userId)
    {
        if (account.OwnerUserId != userId)
            throw new DomainException("Domain:UsuarioNaoAutorizado");
    }

    private async Task PublishDomainEventsAsync(Domain.Entities.EscrowAccount account, CancellationToken cancellationToken)
    {
        foreach (var domainEvent in account.DomainEvents)
        {
            await _publisher.Publish(domainEvent, cancellationToken);
        }
    }
}
