using AssistenteExecutivo.Application.Commands.Credits;
using AssistenteExecutivo.Application.Interfaces;
using AssistenteExecutivo.Domain.Interfaces;
using AssistenteExecutivo.Domain.ValueObjects;
using MediatR;

namespace AssistenteExecutivo.Application.Handlers.Credits;

public class ConsumeCreditsCommandHandler : IRequestHandler<ConsumeCreditsCommand, ConsumeCreditsResult>
{
    private readonly ICreditWalletRepository _walletRepository;
    private readonly IClock _clock;
    private readonly IUnitOfWork _unitOfWork;

    public ConsumeCreditsCommandHandler(
        ICreditWalletRepository walletRepository,
        IClock clock,
        IUnitOfWork unitOfWork)
    {
        _walletRepository = walletRepository;
        _clock = clock;
        _unitOfWork = unitOfWork;
    }

    public async Task<ConsumeCreditsResult> Handle(ConsumeCreditsCommand request, CancellationToken cancellationToken)
    {
        // Validar OwnerUserId
        if (request.OwnerUserId == Guid.Empty)
            throw new ArgumentException("OwnerUserId é obrigatório", nameof(request.OwnerUserId));

        // Validar Amount
        if (request.Amount <= 0)
            throw new ArgumentException("Amount deve ser maior que zero", nameof(request.Amount));

        // Validar IdempotencyKey
        if (string.IsNullOrWhiteSpace(request.IdempotencyKey))
            throw new ArgumentException("IdempotencyKey é obrigatório", nameof(request.IdempotencyKey));

        // Validar Purpose
        if (string.IsNullOrWhiteSpace(request.Purpose))
            throw new ArgumentException("Purpose é obrigatório", nameof(request.Purpose));

        // Obter ou criar wallet
        var wallet = await _walletRepository.GetOrCreateAsync(request.OwnerUserId, cancellationToken);

        // Verificar idempotência antes de executar
        var idempotencyKey = IdempotencyKey.Create(request.IdempotencyKey);
        var existingTransaction = wallet.Transactions
            .FirstOrDefault(t => t.IdempotencyKey == idempotencyKey &&
                                 t.Type == Domain.Enums.CreditTransactionType.Consume);

        bool wasIdempotent = existingTransaction != null;

        // Se já existe transação Consume com essa idempotency key, retornar resultado idempotente
        if (wasIdempotent)
        {
            return new ConsumeCreditsResult
            {
                OwnerUserId = wallet.OwnerUserId,
                NewBalance = wallet.Balance.Value,
                TransactionId = existingTransaction!.TransactionId,
                WasIdempotent = true
            };
        }

        // Criar CreditAmount
        var creditAmount = CreditAmount.Create(request.Amount);

        // Executar Consume no domínio (valida saldo e idempotência internamente)
        wallet.Consume(creditAmount, idempotencyKey, request.Purpose, _clock);

        // Atualizar wallet (GetOrCreateAsync já adiciona novas wallets ao contexto)
        // Se a wallet já está sendo rastreada, o EF Core detectará as novas transações automaticamente
        await _walletRepository.UpdateAsync(wallet, cancellationToken);

        // Salvar mudanças
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        // Obter a última transação criada
        var lastTransaction = wallet.Transactions.LastOrDefault();

        return new ConsumeCreditsResult
        {
            OwnerUserId = wallet.OwnerUserId,
            NewBalance = wallet.Balance.Value,
            TransactionId = lastTransaction?.TransactionId ?? Guid.Empty,
            WasIdempotent = false
        };
    }
}
