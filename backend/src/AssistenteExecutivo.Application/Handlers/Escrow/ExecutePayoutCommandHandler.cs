using System.Linq;
using AssistenteExecutivo.Application.Commands.Escrow;
using AssistenteExecutivo.Application.Interfaces;
using AssistenteExecutivo.Domain.Enums;
using AssistenteExecutivo.Domain.Exceptions;
using AssistenteExecutivo.Domain.Interfaces;
using MediatR;

namespace AssistenteExecutivo.Application.Handlers.Escrow;

public class ExecutePayoutCommandHandler : IRequestHandler<ExecutePayoutCommand, Unit>
{
    private readonly IEscrowAccountRepository _escrowAccountRepository;
    private readonly IPaymentGateway _paymentGateway;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IClock _clock;
    private readonly IPublisher _publisher;

    public ExecutePayoutCommandHandler(
        IEscrowAccountRepository escrowAccountRepository,
        IPaymentGateway paymentGateway,
        IUnitOfWork unitOfWork,
        IClock clock,
        IPublisher publisher)
    {
        _escrowAccountRepository = escrowAccountRepository;
        _paymentGateway = paymentGateway;
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

        var payoutResult = await _paymentGateway.ExecuteEscrowPayoutAsync(
            account.EscrowAccountId,
            transaction.TransactionId,
            transaction.Amount,
            account.StripeConnectedAccountId,
            cancellationToken);

        var normalizedStatus = payoutResult.Status?.ToLowerInvariant() ?? string.Empty;
        if (!(normalizedStatus.Contains("succeeded") || normalizedStatus.Contains("paid") || normalizedStatus.Contains("completed")))
            throw new DomainException("Domain:PayoutNaoExecutado");

        var transferId = string.IsNullOrWhiteSpace(request.StripeTransferId)
            ? payoutResult.TransferId
            : request.StripeTransferId;

        account.MarkPayoutExecuted(request.TransactionId, transferId, _clock);

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
