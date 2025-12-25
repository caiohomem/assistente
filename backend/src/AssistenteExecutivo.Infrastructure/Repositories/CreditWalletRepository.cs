using AssistenteExecutivo.Application.Interfaces;
using AssistenteExecutivo.Domain.Entities;
using AssistenteExecutivo.Domain.Interfaces;
using AssistenteExecutivo.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;

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
        
        // #region agent log
        try { System.IO.File.AppendAllText(@"c:\Projects\AssistenteExecutivo\.cursor\debug.log", JsonSerializer.Serialize(new { sessionId = "debug-session", runId = "run1", hypothesisId = "C", location = "CreditWalletRepository.cs:25", message = "GetByOwnerIdAsync result", data = new { walletFound = wallet != null, ownerUserId = ownerUserId.ToString(), transactionCount = wallet?.Transactions.Count ?? 0 }, timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() }) + "\n"); } catch { }
        // #endregion
        
        return wallet;
    }

    public async Task AddAsync(CreditWallet wallet, CancellationToken cancellationToken = default)
    {
        await _context.CreditWallets.AddAsync(wallet, cancellationToken);
    }

    public async Task UpdateAsync(CreditWallet wallet, CancellationToken cancellationToken = default)
    {
        // #region agent log
        try { System.IO.File.AppendAllText(@"c:\Projects\AssistenteExecutivo\.cursor\debug.log", JsonSerializer.Serialize(new { sessionId = "debug-session", runId = "run1", hypothesisId = "A,C,E", location = "CreditWalletRepository.cs:32", message = "UpdateAsync entry", data = new { walletOwnerUserId = wallet.OwnerUserId.ToString(), transactionCount = wallet.Transactions.Count }, timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() }) + "\n"); } catch { }
        // #endregion
        
        var walletEntry = _context.Entry(wallet);
        
        // #region agent log
        try { System.IO.File.AppendAllText(@"c:\Projects\AssistenteExecutivo\.cursor\debug.log", JsonSerializer.Serialize(new { sessionId = "debug-session", runId = "run1", hypothesisId = "A,C,E", location = "CreditWalletRepository.cs:37", message = "Entry state before processing", data = new { entryState = walletEntry.State.ToString(), isModified = walletEntry.State == EntityState.Modified, hasPropertyChanges = walletEntry.Properties.Any(p => p.IsModified) }, timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() }) + "\n"); } catch { }
        // #endregion

        if (walletEntry.State == EntityState.Detached)
        {
            _context.CreditWallets.Attach(wallet);
            walletEntry.State = EntityState.Unchanged;
            
            // #region agent log
            try { System.IO.File.AppendAllText(@"c:\Projects\AssistenteExecutivo\.cursor\debug.log", JsonSerializer.Serialize(new { sessionId = "debug-session", runId = "run1", hypothesisId = "C", location = "CreditWalletRepository.cs:44", message = "Attached wallet and set to Unchanged", data = new { }, timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() }) + "\n"); } catch { }
            // #endregion
        }

        // Se já está sendo rastreada, garantir que não está marcada como Modified
        // (apenas se não houver mudanças reais nas propriedades da wallet)
        if (walletEntry.State == EntityState.Modified)
        {
            // Verificar se realmente há mudanças nas propriedades da wallet (não apenas nas transações)
            var hasPropertyChanges = walletEntry.Properties.Any(p => p.IsModified);
            
            // #region agent log
            try { System.IO.File.AppendAllText(@"c:\Projects\AssistenteExecutivo\.cursor\debug.log", JsonSerializer.Serialize(new { sessionId = "debug-session", runId = "run1", hypothesisId = "A,E", location = "CreditWalletRepository.cs:54", message = "Checking property changes", data = new { hasPropertyChanges = hasPropertyChanges, modifiedProperties = walletEntry.Properties.Where(p => p.IsModified).Select(p => p.Metadata.Name).ToList() }, timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() }) + "\n"); } catch { }
            // #endregion
            
            if (!hasPropertyChanges)
            {
                // Se não há mudanças nas propriedades, apenas nas transações, marcar como Unchanged
                walletEntry.State = EntityState.Unchanged;
                
                // #region agent log
                try { System.IO.File.AppendAllText(@"c:\Projects\AssistenteExecutivo\.cursor\debug.log", JsonSerializer.Serialize(new { sessionId = "debug-session", runId = "run1", hypothesisId = "A,E", location = "CreditWalletRepository.cs:63", message = "Set entry state to Unchanged (no property changes)", data = new { }, timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() }) + "\n"); } catch { }
                // #endregion
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

            // #region agent log
            try { System.IO.File.AppendAllText(@"c:\Projects\AssistenteExecutivo\.cursor\debug.log", JsonSerializer.Serialize(new { sessionId = "debug-session", runId = "run1", hypothesisId = "A", location = "CreditWalletRepository.cs:57", message = "Wallet is Added, added transactions", data = new { }, timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() }) + "\n"); } catch { }
            // #endregion
            
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
            
            // #region agent log
            try { System.IO.File.AppendAllText(@"c:\Projects\AssistenteExecutivo\.cursor\debug.log", JsonSerializer.Serialize(new { sessionId = "debug-session", runId = "run1", hypothesisId = "F", location = "CreditWalletRepository.cs:115", message = "Transaction details", data = new { transactionId = transaction.TransactionId.ToString(), amount = transaction.Amount?.Value, amountIsNull = transaction.Amount == null, type = transaction.Type.ToString(), state = initialState }, timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() }) + "\n"); } catch { }
            // #endregion

            if (transactionEntry.State == EntityState.Detached)
            {
                // Nova transação não rastreada - adicionar
                _context.CreditTransactions.Add(transaction);
                newTransactionsCount++;
                transactionStates.Add(new { 
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
                
                // #region agent log
                try { 
                    var addedEntry = _context.Entry(transaction);
                    System.IO.File.AppendAllText(@"c:\Projects\AssistenteExecutivo\.cursor\debug.log", JsonSerializer.Serialize(new { sessionId = "debug-session", runId = "run1", hypothesisId = "F", location = "CreditWalletRepository.cs:138", message = "After Add transaction", data = new { transactionId = transaction.TransactionId.ToString(), amount = transaction.Amount?.Value, amountIsNull = transaction.Amount == null, entryState = addedEntry.State.ToString() }, timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() }) + "\n"); 
                } catch { }
                // #endregion
                
                newTransactionsCount++;
                transactionStates.Add(new { 
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
            
            transactionStates.Add(new { 
                transactionId = transaction.TransactionId.ToString(), 
                initialState = initialState,
                finalState = transactionEntry.State.ToString(),
                existsInDb = existsInDb
            });
        }
        
        // #region agent log
        try { System.IO.File.AppendAllText(@"c:\Projects\AssistenteExecutivo\.cursor\debug.log", JsonSerializer.Serialize(new { sessionId = "debug-session", runId = "run1", hypothesisId = "A,E", location = "CreditWalletRepository.cs:155", message = "UpdateAsync exit", data = new { finalEntryState = walletEntry.State.ToString(), newTransactionsAdded = newTransactionsCount, transactionStates = transactionStates }, timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() }) + "\n"); } catch { }
        // #endregion
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
