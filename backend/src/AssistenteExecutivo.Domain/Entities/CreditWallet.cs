using AssistenteExecutivo.Domain.DomainEvents;
using AssistenteExecutivo.Domain.Enums;
using AssistenteExecutivo.Domain.Exceptions;
using AssistenteExecutivo.Domain.Interfaces;
using AssistenteExecutivo.Domain.ValueObjects;

namespace AssistenteExecutivo.Domain.Entities;

public class CreditWallet
{
    private readonly List<CreditTransaction> _transactions = new();
    private readonly List<IDomainEvent> _domainEvents = new();

    private CreditWallet() { } // EF Core

    public CreditWallet(
        Guid ownerUserId,
        IClock clock)
    {
        if (ownerUserId == Guid.Empty)
            throw new DomainException("Domain:OwnerUserIdObrigatorio");

        if (clock == null)
            throw new DomainException("Domain:ClockObrigatorio");

        OwnerUserId = ownerUserId;
        CreatedAt = clock.UtcNow;
    }

    public Guid OwnerUserId { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public IReadOnlyCollection<CreditTransaction> Transactions => _transactions.AsReadOnly();

    public CreditAmount Balance
    {
        get
        {
            // Calcular saldo usando decimal para evitar exceção quando resultado seria negativo
            // O saldo nunca deve ser negativo, então retornamos zero se o cálculo resultar em negativo
            decimal balanceValue = 0m;
            foreach (var transaction in _transactions)
            {
                switch (transaction.Type)
                {
                    case CreditTransactionType.Grant:
                    case CreditTransactionType.Purchase:
                    case CreditTransactionType.Refund:
                        balanceValue += transaction.Amount.Value;
                        break;
                    case CreditTransactionType.Reserve:
                    case CreditTransactionType.Consume:
                    case CreditTransactionType.Expire:
                        balanceValue -= transaction.Amount.Value;
                        break;
                }
            }

            // Garantir que o saldo nunca seja negativo (retornar zero se for negativo)
            if (balanceValue < 0)
                return CreditAmount.Zero;

            return CreditAmount.Create(balanceValue);
        }
    }

    public IReadOnlyCollection<IDomainEvent> DomainEvents => _domainEvents.AsReadOnly();

    public void Grant(CreditAmount amount, string? reason, IClock clock)
    {
        if (amount == null)
            throw new DomainException("Domain:CreditAmountObrigatorio");

        if (amount <= CreditAmount.Zero)
            throw new DomainException("Domain:CreditAmountDeveSerPositivo");

        var transaction = new CreditTransaction(
            Guid.NewGuid(),
            OwnerUserId,
            CreditTransactionType.Grant,
            amount,
            reason,
            clock.UtcNow);

        _transactions.Add(transaction);
        _domainEvents.Add(new CreditsGranted(OwnerUserId, amount, clock.UtcNow));
    }

    public void Purchase(CreditAmount amount, string? reason, IClock clock)
    {
        if (amount == null)
            throw new DomainException("Domain:CreditAmountObrigatorio");

        if (amount <= CreditAmount.Zero)
            throw new DomainException("Domain:CreditAmountDeveSerPositivo");

        var transaction = new CreditTransaction(
            Guid.NewGuid(),
            OwnerUserId,
            CreditTransactionType.Purchase,
            amount,
            reason,
            clock.UtcNow);

        _transactions.Add(transaction);
        _domainEvents.Add(new CreditsGranted(OwnerUserId, amount, clock.UtcNow));
    }

    public void Reserve(
        CreditAmount amount,
        IdempotencyKey idempotencyKey,
        string purpose,
        IClock clock)
    {
        if (amount == null)
            throw new DomainException("Domain:CreditAmountObrigatorio");

        if (idempotencyKey == null)
            throw new DomainException("Domain:IdempotencyKeyObrigatorio");

        // Verificar idempotência
        if (_transactions.Any(t => t.IdempotencyKey == idempotencyKey))
            return; // Já processado, idempotente

        // Verificar saldo
        var availableBalance = Balance;
        if (availableBalance < amount)
            throw new DomainException("Domain:SaldoInsuficiente");

        var transaction = new CreditTransaction(
            Guid.NewGuid(),
            OwnerUserId,
            CreditTransactionType.Reserve,
            amount,
            purpose,
            clock.UtcNow,
            idempotencyKey);

        _transactions.Add(transaction);
        _domainEvents.Add(new CreditsReserved(OwnerUserId, amount, purpose, clock.UtcNow));
    }

    public void Consume(
        CreditAmount amount,
        IdempotencyKey idempotencyKey,
        string purpose,
        IClock clock)
    {
        if (amount == null)
            throw new DomainException("Domain:CreditAmountObrigatorio");

        if (idempotencyKey == null)
            throw new DomainException("Domain:IdempotencyKeyObrigatorio");

        // Verificar idempotência
        if (_transactions.Any(t => t.IdempotencyKey == idempotencyKey && t.Type == CreditTransactionType.Consume))
            return; // Já processado, idempotente

        // Verificar saldo (incluindo reservas)
        var availableBalance = Balance;
        if (availableBalance < amount)
            throw new DomainException("Domain:SaldoInsuficiente");

        var transaction = new CreditTransaction(
            Guid.NewGuid(),
            OwnerUserId,
            CreditTransactionType.Consume,
            amount,
            purpose,
            clock.UtcNow,
            idempotencyKey);

        _transactions.Add(transaction);
        _domainEvents.Add(new CreditsConsumed(OwnerUserId, amount, purpose, clock.UtcNow));
    }

    public void Refund(
        CreditAmount amount,
        IdempotencyKey idempotencyKey,
        string purpose,
        IClock clock)
    {
        if (amount == null)
            throw new DomainException("Domain:CreditAmountObrigatorio");

        if (idempotencyKey == null)
            throw new DomainException("Domain:IdempotencyKeyObrigatorio");

        // Verificar idempotência
        if (_transactions.Any(t => t.IdempotencyKey == idempotencyKey && t.Type == CreditTransactionType.Refund))
            return; // Já processado, idempotente

        var transaction = new CreditTransaction(
            Guid.NewGuid(),
            OwnerUserId,
            CreditTransactionType.Refund,
            amount,
            purpose,
            clock.UtcNow,
            idempotencyKey);

        _transactions.Add(transaction);
        _domainEvents.Add(new CreditsRefunded(OwnerUserId, amount, purpose, clock.UtcNow));
    }

    public void ClearDomainEvents()
    {
        _domainEvents.Clear();
    }
}

public class CreditTransaction
{
    private CreditTransaction() { } // EF Core

    public CreditTransaction(
        Guid transactionId,
        Guid ownerUserId,
        CreditTransactionType type,
        CreditAmount amount,
        string? reason,
        DateTime occurredAt,
        IdempotencyKey? idempotencyKey = null)
    {
        if (amount == null)
            throw new DomainException("Domain:CreditAmountObrigatorio");

        TransactionId = transactionId;
        OwnerUserId = ownerUserId;
        Type = type;
        Amount = CreditAmount.Create(amount.Value);
        Reason = reason;
        OccurredAt = occurredAt;
        IdempotencyKey = idempotencyKey;
    }

    public Guid TransactionId { get; private set; }
    public Guid OwnerUserId { get; private set; }
    public CreditTransactionType Type { get; private set; }
    public CreditAmount Amount { get; private set; } = null!;
    public string? Reason { get; private set; }
    public DateTime OccurredAt { get; private set; }
    public IdempotencyKey? IdempotencyKey { get; private set; }
}

