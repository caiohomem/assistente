using AssistenteExecutivo.Application.Commands.Credits;
using AssistenteExecutivo.Application.Interfaces;
using AssistenteExecutivo.Domain.Entities;
using AssistenteExecutivo.Domain.Interfaces;
using AssistenteExecutivo.Domain.ValueObjects;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace AssistenteExecutivo.Application.Handlers.Credits;

public class PurchaseCreditPackageCommandHandler : IRequestHandler<PurchaseCreditPackageCommand, PurchaseCreditPackageResult>
{
    private readonly ICreditWalletRepository _walletRepository;
    private readonly IApplicationDbContext _context;
    private readonly IClock _clock;
    private readonly IUnitOfWork _unitOfWork;

    public PurchaseCreditPackageCommandHandler(
        ICreditWalletRepository walletRepository,
        IApplicationDbContext context,
        IClock clock,
        IUnitOfWork unitOfWork)
    {
        _walletRepository = walletRepository;
        _context = context;
        _clock = clock;
        _unitOfWork = unitOfWork;
    }

    public async Task<PurchaseCreditPackageResult> Handle(PurchaseCreditPackageCommand request, CancellationToken cancellationToken)
    {
        // Validar OwnerUserId
        if (request.OwnerUserId == Guid.Empty)
            throw new ArgumentException("OwnerUserId é obrigatório", nameof(request.OwnerUserId));

        // Validar PackageId
        if (request.PackageId == Guid.Empty)
            throw new ArgumentException("PackageId é obrigatório", nameof(request.PackageId));

        // Buscar pacote
        var package = await _context.CreditPackages
            .FirstOrDefaultAsync(p => p.PackageId == request.PackageId, cancellationToken);

        if (package == null)
            throw new InvalidOperationException("Pacote não encontrado");

        if (!package.IsActive)
            throw new InvalidOperationException("Pacote não está ativo");

        // Validar amount (não pode ser ilimitado para compra direta)
        if (package.Amount < 0)
            throw new InvalidOperationException("Pacotes ilimitados não podem ser comprados diretamente");

        // Obter ou criar wallet
        var wallet = await _walletRepository.GetOrCreateAsync(request.OwnerUserId, cancellationToken);

        // Criar CreditAmount
        var creditAmount = CreditAmount.Create(package.Amount);

        // Executar Purchase no domínio
        var reason = $"Compra de pacote: {package.Name}";
        wallet.Purchase(creditAmount, reason, _clock);

        // Atualizar wallet
        await _walletRepository.UpdateAsync(wallet, cancellationToken);

        // Salvar mudanças
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        // Obter a última transação criada
        var lastTransaction = wallet.Transactions.LastOrDefault();

        return new PurchaseCreditPackageResult
        {
            OwnerUserId = wallet.OwnerUserId,
            NewBalance = wallet.Balance.Value,
            TransactionId = lastTransaction?.TransactionId ?? Guid.Empty,
            PackageName = package.Name,
            CreditsAdded = package.Amount
        };
    }
}


