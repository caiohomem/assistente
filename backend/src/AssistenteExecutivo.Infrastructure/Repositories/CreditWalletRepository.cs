using AssistenteExecutivo.Application.Interfaces;
using AssistenteExecutivo.Domain.Entities;
using AssistenteExecutivo.Domain.Interfaces;
using AssistenteExecutivo.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace AssistenteExecutivo.Infrastructure.Repositories;

public class CreditWalletRepository : ICreditWalletRepository
{
    private readonly ApplicationDbContext _context;
    private readonly IClock _clock;

    public CreditWalletRepository(ApplicationDbContext context, IClock clock)
    {
        _context = context;
        _clock = clock;
    }

    public async Task<CreditWallet?> GetByOwnerIdAsync(Guid ownerUserId, CancellationToken cancellationToken = default)
    {
        var wallet = await _context.CreditWallets
            .Include(w => w.Transactions)
            .AsTracking() // Garantir que seja rastreada para detectar mudanças
            .FirstOrDefaultAsync(w => w.OwnerUserId == ownerUserId, cancellationToken);

        return wallet;
    }

    public async Task AddAsync(CreditWallet wallet, CancellationToken cancellationToken = default)
    {
        await _context.CreditWallets.AddAsync(wallet, cancellationToken);
    }

    public async Task UpdateAsync(CreditWallet wallet, CancellationToken cancellationToken = default)
    {
        var walletEntry = _context.Entry(wallet);

        if (walletEntry.State == EntityState.Detached)
        {
            _context.CreditWallets.Attach(wallet);
            walletEntry.State = EntityState.Unchanged;

        }

        // Se já está sendo rastreada, garantir que não está marcada como Modified
        // (apenas se não houver mudanças reais nas propriedades da wallet)
        if (walletEntry.State == EntityState.Modified)
        {
            // Verificar se realmente há mudanças nas propriedades da wallet (não apenas nas transações)
            var hasPropertyChanges = walletEntry.Properties.Any(p => p.IsModified);

            if (!hasPropertyChanges)
            {
                // Se não há mudanças nas propriedades, apenas nas transações, marcar como Unchanged
                walletEntry.State = EntityState.Unchanged;

            }
        }

        if (walletEntry.State == EntityState.Added)
        {
            foreach (var transaction in wallet.Transactions)
            {
                if (_context.Entry(transaction).State == EntityState.Detached)
                {
                    _context.CreditTransactions.Add(transaction);
                }
            }

            return;
        }

        // Buscar todos os TransactionIds existentes no banco de uma vez (otimização)
        var transactionIds = wallet.Transactions.Select(t => t.TransactionId).ToList();
        var existingTransactionIds = await _context.CreditTransactions
            .Where(t => transactionIds.Contains(t.TransactionId))
            .Select(t => t.TransactionId)
            .ToListAsync(cancellationToken);
        var existingIdsSet = new HashSet<Guid>(existingTransactionIds);

        var newTransactionsCount = 0;
        var transactionStates = new List<object>();
        foreach (var transaction in wallet.Transactions)
        {
            var transactionEntry = _context.Entry(transaction);
            var initialState = transactionEntry.State.ToString();
            var existsInDb = existingIdsSet.Contains(transaction.TransactionId);

            if (transactionEntry.State == EntityState.Detached)
            {
                // Nova transação não rastreada - adicionar
                _context.CreditTransactions.Add(transaction);
                newTransactionsCount++;
                transactionStates.Add(new
                {
                    transactionId = transaction.TransactionId.ToString(),
                    initialState = initialState,
                    finalState = "Added",
                    existsInDb = existsInDb
                });
                continue;
            }

            // Se a transação não existe no banco mas está rastreada como Unchanged ou Modified,
            // significa que é nova e precisa ser adicionada
            // IMPORTANTE: Remover do tracker e adicionar novamente para garantir que owned types sejam mapeados corretamente
            if (!existsInDb && (transactionEntry.State == EntityState.Unchanged || transactionEntry.State == EntityState.Modified))
            {
                // Remover do tracker e adicionar novamente para garantir mapeamento correto de owned types
                transactionEntry.State = EntityState.Detached;
                _context.CreditTransactions.Add(transaction);

                newTransactionsCount++;
                transactionStates.Add(new
                {
                    transactionId = transaction.TransactionId.ToString(),
                    initialState = initialState,
                    finalState = "Added",
                    existsInDb = existsInDb
                });
                continue;
            }

            if (transactionEntry.State == EntityState.Modified && existsInDb)
            {
                // Transação existe no banco, não deve estar Modified
                transactionEntry.State = EntityState.Unchanged;
            }

            transactionStates.Add(new
            {
                transactionId = transaction.TransactionId.ToString(),
                initialState = initialState,
                finalState = transactionEntry.State.ToString(),
                existsInDb = existsInDb
            });
        }

    }

    public async Task<CreditWallet> GetOrCreateAsync(Guid ownerUserId, CancellationToken cancellationToken = default)
    {
        var wallet = await GetByOwnerIdAsync(ownerUserId, cancellationToken);

        if (wallet != null)
        {
            return wallet;
        }

        wallet = new CreditWallet(ownerUserId, _clock);
        await AddAsync(wallet, cancellationToken);
        return wallet;
    }
}
