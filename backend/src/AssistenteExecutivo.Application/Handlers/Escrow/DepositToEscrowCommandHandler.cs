using AssistenteExecutivo.Application.Commands.Escrow;
using AssistenteExecutivo.Application.DTOs;
using AssistenteExecutivo.Application.Interfaces;
using AssistenteExecutivo.Domain.Enums;
using AssistenteExecutivo.Domain.Exceptions;
using AssistenteExecutivo.Domain.Interfaces;
using AssistenteExecutivo.Domain.ValueObjects;
using MediatR;

namespace AssistenteExecutivo.Application.Handlers.Escrow;

public class DepositToEscrowCommandHandler : IRequestHandler<DepositToEscrowCommand, EscrowDepositResult>
{
    private readonly IEscrowAccountRepository _escrowAccountRepository;
    private readonly IPaymentGateway _paymentGateway;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IClock _clock;
    private readonly IPublisher _publisher;

    public DepositToEscrowCommandHandler(
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

    public async Task<EscrowDepositResult> Handle(DepositToEscrowCommand request, CancellationToken cancellationToken)
    {
        if (request.RequestedBy == Guid.Empty)
            throw new DomainException("Domain:UsuarioSolicitanteObrigatorio");
        if (request.Amount <= 0)
            throw new DomainException("Domain:ValorDepositoObrigatorio");

        var account = await _escrowAccountRepository.GetByIdAsync(request.EscrowAccountId, cancellationToken)
            ?? throw new DomainException("Domain:ContaEscrowNaoEncontrada");

        EnsureOwner(account, request.RequestedBy);

        var amount = Money.Create(request.Amount, string.IsNullOrWhiteSpace(request.Currency) ? account.Currency : request.Currency!);
        var transactionId = request.TransactionId == Guid.Empty ? Guid.NewGuid() : request.TransactionId;
        var description = string.IsNullOrWhiteSpace(request.Description)
            ? $"Depósito escrow #{transactionId.ToString()[..8]}"
            : request.Description!;

        var paymentIntent = await _paymentGateway.CreateEscrowDepositIntentAsync(
            account.EscrowAccountId,
            amount,
            description,
            cancellationToken);

        var transaction = account.RegisterDeposit(
            transactionId,
            amount,
            description,
            EscrowTransactionStatus.Completed,
            paymentIntent.PaymentIntentId,
            request.IdempotencyKey,
            _clock);

        // Explicitly add the new transaction to the context
        await _escrowAccountRepository.AddTransactionAsync(transaction, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        await PublishDomainEventsAsync(account, cancellationToken);
        account.ClearDomainEvents();

        return new EscrowDepositResult
        {
            TransactionId = transaction.TransactionId,
            PaymentIntentId = paymentIntent.PaymentIntentId,
            ClientSecret = paymentIntent.ClientSecret
        };
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
