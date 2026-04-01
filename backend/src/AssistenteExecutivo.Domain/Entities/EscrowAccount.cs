using AssistenteExecutivo.Domain.DomainEvents;
using AssistenteExecutivo.Domain.Enums;
using AssistenteExecutivo.Domain.Exceptions;
using AssistenteExecutivo.Domain.Interfaces;
using AssistenteExecutivo.Domain.ValueObjects;

namespace AssistenteExecutivo.Domain.Entities;

public class EscrowAccount
{
    private readonly List<EscrowTransaction> _transactions = new();
    private readonly List<IDomainEvent> _domainEvents = new();

    private EscrowAccount() { } // EF Core

    private EscrowAccount(
        Guid escrowAccountId,
        Guid agreementId,
        Guid ownerUserId,
        string currency,
        IClock clock)
    {
        if (escrowAccountId == Guid.Empty)
            throw new DomainException("Domain:EscrowAccountIdObrigatorio");

        if (agreementId == Guid.Empty)
            throw new DomainException("Domain:AgreementIdObrigatorio");

        if (ownerUserId == Guid.Empty)
            throw new DomainException("Domain:OwnerUserIdObrigatorio");

        if (string.IsNullOrWhiteSpace(currency))
            throw new DomainException("Domain:MoedaObrigatoria");

        EscrowAccountId = escrowAccountId;
        AgreementId = agreementId;
        OwnerUserId = ownerUserId;
        Currency = currency.ToUpperInvariant();
        Status = EscrowAccountStatus.Active;
        CreatedAt = clock.UtcNow;
        UpdatedAt = clock.UtcNow;

        _domainEvents.Add(new EscrowAccountCreated(EscrowAccountId, AgreementId, OwnerUserId, clock.UtcNow));
    }

    public Guid EscrowAccountId { get; private set; }
    public Guid AgreementId { get; private set; }
    public Guid OwnerUserId { get; private set; }
    public string Currency { get; private set; } = "BRL";
    public EscrowAccountStatus Status { get; private set; }
    public string? StripeConnectedAccountId { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime UpdatedAt { get; private set; }
    public IReadOnlyCollection<EscrowTransaction> Transactions => _transactions.AsReadOnly();
    public IReadOnlyCollection<IDomainEvent> DomainEvents => _domainEvents.AsReadOnly();

    public Money Balance => CalculateBalance();

    public static EscrowAccount Create(
        Guid escrowAccountId,
        Guid agreementId,
        Guid ownerUserId,
        string currency,
        IClock clock)
    {
        return new EscrowAccount(escrowAccountId, agreementId, ownerUserId, currency, clock);
    }

    public void ConnectStripeAccount(string connectedAccountId)
    {
        if (string.IsNullOrWhiteSpace(connectedAccountId))
            throw new DomainException("Domain:StripeConnectedAccountObrigatorio");

        StripeConnectedAccountId = connectedAccountId.Trim();
        UpdatedAt = DateTime.UtcNow;
    }

    public EscrowTransaction RegisterDeposit(
        Guid transactionId,
        Money amount,
        string? description,
        EscrowTransactionStatus status,
        string? paymentIntentId,
        string? idempotencyKey,
        IClock clock)
    {
        EnsureSameCurrency(amount);

        // Evitar duplicidade por idempotencyKey
        if (!string.IsNullOrWhiteSpace(idempotencyKey))
        {
            if (_transactions.Any(t => t.IdempotencyKey == idempotencyKey))
                return _transactions.First(t => t.IdempotencyKey == idempotencyKey);
        }

        var transaction = EscrowTransaction.CreateDeposit(
            transactionId,
            EscrowAccountId,
            amount,
            description,
            status,
            paymentIntentId,
            idempotencyKey,
            clock);

        _transactions.Add(transaction);
        UpdatedAt = clock.UtcNow;

        if (status == EscrowTransactionStatus.Completed)
        {
            _domainEvents.Add(new EscrowDepositReceived(
                EscrowAccountId,
                transaction.TransactionId,
                amount.Amount,
                amount.Currency,
                clock.UtcNow));
        }

        return transaction;
    }

    public EscrowTransaction RequestPayout(
        Guid transactionId,
        Guid? partyId,
        Money amount,
        string? description,
        PayoutApprovalType approvalType,
        string? idempotencyKey,
        IClock clock)
    {
        EnsureSameCurrency(amount);

        if (Status != EscrowAccountStatus.Active)
            throw new DomainException("Domain:EscrowContaNaoAtiva");

        var availableBalance = GetAvailableBalance();
        if (availableBalance < amount)
            throw new DomainException("Domain:SaldoEscrowInsuficiente");

        if (!string.IsNullOrWhiteSpace(idempotencyKey))
        {
            var existing = _transactions.FirstOrDefault(t => t.IdempotencyKey == idempotencyKey);
            if (existing != null)
                return existing;
        }

        var transaction = EscrowTransaction.CreatePayout(
            transactionId,
            EscrowAccountId,
            partyId,
            amount,
            description,
            approvalType,
            idempotencyKey,
            clock);

        _transactions.Add(transaction);
        UpdatedAt = clock.UtcNow;

        _domainEvents.Add(new PayoutRequested(
            EscrowAccountId,
            transaction.TransactionId,
            partyId ?? Guid.Empty,
            amount.Amount,
            amount.Currency,
            approvalType,
            clock.UtcNow));

        return transaction;
    }

    public void ApprovePayout(Guid transactionId, Guid approvedBy, IClock clock)
    {
        var transaction = GetTransaction(transactionId);
        transaction.MarkApproved(approvedBy, clock);
        UpdatedAt = clock.UtcNow;

        _domainEvents.Add(new PayoutApproved(
            EscrowAccountId,
            transaction.TransactionId,
            approvedBy,
            transaction.ApprovalType ?? PayoutApprovalType.Automatic,
            clock.UtcNow));
    }

    public void RejectPayout(Guid transactionId, Guid rejectedBy, string reason, IClock clock)
    {
        var transaction = GetTransaction(transactionId);
        transaction.MarkRejected(rejectedBy, reason, clock);
        UpdatedAt = clock.UtcNow;

        _domainEvents.Add(new PayoutRejected(
            EscrowAccountId,
            transaction.TransactionId,
            rejectedBy,
            reason,
            clock.UtcNow));
    }

    public void MarkPayoutExecuted(Guid transactionId, string? stripeTransferId, IClock clock)
    {
        var transaction = GetTransaction(transactionId);
        transaction.MarkCompleted(stripeTransferId, clock);
        UpdatedAt = clock.UtcNow;

        _domainEvents.Add(new PayoutExecuted(
            EscrowAccountId,
            transaction.TransactionId,
            transaction.Amount.Amount,
            transaction.Amount.Currency,
            stripeTransferId,
            clock.UtcNow));
    }

    public void MarkTransactionDisputed(Guid transactionId, string reason, IClock clock)
    {
        var transaction = GetTransaction(transactionId);
        transaction.MarkDisputed(reason, clock);
        UpdatedAt = clock.UtcNow;
    }

    public void Suspend()
    {
        Status = EscrowAccountStatus.Suspended;
        UpdatedAt = DateTime.UtcNow;
    }

    public void Close()
    {
        Status = EscrowAccountStatus.Closed;
        UpdatedAt = DateTime.UtcNow;
    }

    public void ClearDomainEvents() => _domainEvents.Clear();

    private EscrowTransaction GetTransaction(Guid transactionId)
    {
        var transaction = _transactions.FirstOrDefault(t => t.TransactionId == transactionId);
        if (transaction == null)
            throw new DomainException("Domain:TransacaoNaoEncontrada");

        return transaction;
    }

    private Money CalculateBalance()
    {
        decimal balance = 0m;

        foreach (var transaction in _transactions)
        {
            if (transaction.Status != EscrowTransactionStatus.Completed && transaction.Status != EscrowTransactionStatus.Approved)
                continue;

            var amount = transaction.Amount.Amount;
            switch (transaction.Type)
            {
                case EscrowTransactionType.Deposit:
                case EscrowTransactionType.Refund:
                    balance += amount;
                    break;
                case EscrowTransactionType.Payout:
                case EscrowTransactionType.Fee:
                    balance -= amount;
                    break;
            }
        }

        if (balance < 0)
            balance = 0;

        return Money.Create(balance, Currency);
    }

    private void EnsureSameCurrency(Money amount)
    {
        if (amount.Currency != Currency)
            throw new DomainException("Domain:MoedaDiferenteDaContaEscrow");
    }

    private Money GetAvailableBalance()
    {
        var pendingPayouts = _transactions
            .Where(t => t.Type == EscrowTransactionType.Payout && t.Status == EscrowTransactionStatus.Pending)
            .Sum(t => t.Amount.Amount);

        var balanceAmount = Balance.Amount - pendingPayouts;
        if (balanceAmount < 0)
            balanceAmount = 0;

        return Money.Create(balanceAmount, Currency);
    }
}
