using AssistenteExecutivo.Application.DTOs;
using AssistenteExecutivo.Application.Interfaces;
using MediatR;

namespace AssistenteExecutivo.Application.Queries.Credits;

public class ListCreditTransactionsQueryHandler : IRequestHandler<ListCreditTransactionsQuery, List<CreditTransactionDto>>
{
    private readonly ICreditWalletRepository _walletRepository;

    public ListCreditTransactionsQueryHandler(ICreditWalletRepository walletRepository)
    {
        _walletRepository = walletRepository;
    }

    public async Task<List<CreditTransactionDto>> Handle(ListCreditTransactionsQuery request, CancellationToken cancellationToken)
    {
        if (request.OwnerUserId == Guid.Empty)
            throw new ArgumentException("OwnerUserId é obrigatório", nameof(request.OwnerUserId));

        var wallet = await _walletRepository.GetByOwnerIdAsync(request.OwnerUserId, cancellationToken);

        if (wallet == null)
            return new List<CreditTransactionDto>();

        var transactions = wallet.Transactions.AsEnumerable();

        // Filtrar por tipo se especificado
        if (request.Type.HasValue)
        {
            transactions = transactions.Where(t => t.Type == request.Type.Value);
        }

        // Filtrar por data inicial se especificada
        if (request.FromDate.HasValue)
        {
            transactions = transactions.Where(t => t.OccurredAt >= request.FromDate.Value);
        }

        // Filtrar por data final se especificada
        if (request.ToDate.HasValue)
        {
            transactions = transactions.Where(t => t.OccurredAt <= request.ToDate.Value);
        }

        // Ordenar por data (mais recente primeiro)
        transactions = transactions.OrderByDescending(t => t.OccurredAt);

        // Aplicar paginação se especificada
        if (request.Offset.HasValue && request.Offset.Value > 0)
        {
            transactions = transactions.Skip(request.Offset.Value);
        }

        if (request.Limit.HasValue && request.Limit.Value > 0)
        {
            transactions = transactions.Take(request.Limit.Value);
        }

        return transactions.Select(t => new CreditTransactionDto
        {
            TransactionId = t.TransactionId,
            OwnerUserId = t.OwnerUserId,
            Type = t.Type,
            Amount = t.Amount.Value,
            Reason = t.Reason,
            OccurredAt = t.OccurredAt,
            IdempotencyKey = t.IdempotencyKey?.Value
        }).ToList();
    }
}

