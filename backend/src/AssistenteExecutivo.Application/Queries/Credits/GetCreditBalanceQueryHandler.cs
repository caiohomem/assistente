using AssistenteExecutivo.Application.DTOs;
using AssistenteExecutivo.Application.Interfaces;
using AssistenteExecutivo.Application.Queries.Credits;
using MediatR;

namespace AssistenteExecutivo.Application.Queries.Credits;

public class GetCreditBalanceQueryHandler : IRequestHandler<GetCreditBalanceQuery, CreditBalanceDto?>
{
    private readonly ICreditWalletRepository _walletRepository;

    public GetCreditBalanceQueryHandler(ICreditWalletRepository walletRepository)
    {
        _walletRepository = walletRepository;
    }

    public async Task<CreditBalanceDto?> Handle(GetCreditBalanceQuery request, CancellationToken cancellationToken)
    {
        if (request.OwnerUserId == Guid.Empty)
            throw new ArgumentException("OwnerUserId é obrigatório", nameof(request.OwnerUserId));

        var wallet = await _walletRepository.GetByOwnerIdAsync(request.OwnerUserId, cancellationToken);

        if (wallet == null)
            return null;

        return new CreditBalanceDto
        {
            OwnerUserId = wallet.OwnerUserId,
            Balance = wallet.Balance.Value,
            CreatedAt = wallet.CreatedAt,
            TransactionCount = wallet.Transactions.Count
        };
    }
}





