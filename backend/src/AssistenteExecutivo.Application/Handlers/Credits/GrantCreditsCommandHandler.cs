using AssistenteExecutivo.Application.Commands.Credits;
using AssistenteExecutivo.Application.Interfaces;
using AssistenteExecutivo.Domain.Entities;
using AssistenteExecutivo.Domain.Interfaces;
using AssistenteExecutivo.Domain.ValueObjects;
using MediatR;

namespace AssistenteExecutivo.Application.Handlers.Credits;

public class GrantCreditsCommandHandler : IRequestHandler<GrantCreditsCommand, GrantCreditsResult>
{
    private readonly ICreditWalletRepository _walletRepository;
    private readonly IClock _clock;
    private readonly IUnitOfWork _unitOfWork;

    public GrantCreditsCommandHandler(
        ICreditWalletRepository walletRepository,
        IClock clock,
        IUnitOfWork unitOfWork)
    {
        _walletRepository = walletRepository;
        _clock = clock;
        _unitOfWork = unitOfWork;
    }

    public async Task<GrantCreditsResult> Handle(GrantCreditsCommand request, CancellationToken cancellationToken)
    {
        // Validar OwnerUserId
        if (request.OwnerUserId == Guid.Empty)
            throw new ArgumentException("OwnerUserId é obrigatório", nameof(request.OwnerUserId));

        // Validar Amount
        if (request.Amount <= 0)
            throw new ArgumentException("Amount deve ser maior que zero", nameof(request.Amount));

        // Obter ou criar wallet
        var wallet = await _walletRepository.GetOrCreateAsync(request.OwnerUserId, cancellationToken);

        // Criar CreditAmount
        var creditAmount = CreditAmount.Create(request.Amount);

        // Obter saldo antes da operação
        var balanceBefore = wallet.Balance;

        // Executar Grant no domínio
        wallet.Grant(creditAmount, request.Reason, _clock);

        // Atualizar wallet (GetOrCreateAsync já adiciona novas wallets ao contexto)
        await _walletRepository.UpdateAsync(wallet, cancellationToken);

        // Salvar mudanças
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        // Obter a última transação criada
        var lastTransaction = wallet.Transactions.LastOrDefault();

        return new GrantCreditsResult
        {
            OwnerUserId = wallet.OwnerUserId,
            NewBalance = wallet.Balance.Value,
            TransactionId = lastTransaction?.TransactionId ?? Guid.Empty
        };
    }
}
