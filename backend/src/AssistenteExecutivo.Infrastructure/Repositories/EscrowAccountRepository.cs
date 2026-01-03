using AssistenteExecutivo.Application.Interfaces;
using AssistenteExecutivo.Domain.Entities;
using AssistenteExecutivo.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace AssistenteExecutivo.Infrastructure.Repositories;

public class EscrowAccountRepository : IEscrowAccountRepository
{
    private readonly ApplicationDbContext _context;

    public EscrowAccountRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<EscrowAccount?> GetByIdAsync(Guid escrowAccountId, CancellationToken cancellationToken = default)
    {
        return await _context.EscrowAccounts
            .Include(e => e.Transactions)
            .FirstOrDefaultAsync(e => e.EscrowAccountId == escrowAccountId, cancellationToken);
    }

    public async Task<EscrowAccount?> GetByAgreementIdAsync(Guid agreementId, CancellationToken cancellationToken = default)
    {
        return await _context.EscrowAccounts
            .Include(e => e.Transactions)
            .FirstOrDefaultAsync(e => e.AgreementId == agreementId, cancellationToken);
    }

    public async Task<List<EscrowTransaction>> ListTransactionsAsync(Guid escrowAccountId, CancellationToken cancellationToken = default)
    {
        return await _context.EscrowTransactions
            .Where(t => t.EscrowAccountId == escrowAccountId)
            .OrderByDescending(t => t.CreatedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task AddAsync(EscrowAccount escrowAccount, CancellationToken cancellationToken = default)
    {
        await _context.EscrowAccounts.AddAsync(escrowAccount, cancellationToken);
    }

    public Task UpdateAsync(EscrowAccount escrowAccount, CancellationToken cancellationToken = default)
    {
        _context.EscrowAccounts.Update(escrowAccount);
        return Task.CompletedTask;
    }
}
