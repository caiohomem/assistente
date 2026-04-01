using System.Linq;
using AssistenteExecutivo.Application.DTOs;
using AssistenteExecutivo.Application.Interfaces;
using AssistenteExecutivo.Application.Queries.Escrow;
using AssistenteExecutivo.Domain.Exceptions;
using MediatR;

namespace AssistenteExecutivo.Application.Handlers.Escrow;

public class ListEscrowTransactionsQueryHandler : IRequestHandler<ListEscrowTransactionsQuery, List<EscrowTransactionDto>>
{
    private readonly IEscrowAccountRepository _escrowAccountRepository;

    public ListEscrowTransactionsQueryHandler(IEscrowAccountRepository escrowAccountRepository)
    {
        _escrowAccountRepository = escrowAccountRepository;
    }

    public async Task<List<EscrowTransactionDto>> Handle(ListEscrowTransactionsQuery request, CancellationToken cancellationToken)
    {
        if (request.EscrowAccountId == Guid.Empty)
            throw new DomainException("Domain:EscrowAccountIdObrigatorio");

        var account = await _escrowAccountRepository.GetByIdAsync(request.EscrowAccountId, cancellationToken)
            ?? throw new DomainException("Domain:ContaEscrowNaoEncontrada");

        EnsureOwner(account, request.RequestingUserId);

        return account.Transactions
            .OrderByDescending(t => t.CreatedAt)
            .Select(EscrowMapper.MapTransaction)
            .ToList();
    }

    private static void EnsureOwner(Domain.Entities.EscrowAccount account, Guid requestingUserId)
    {
        if (requestingUserId == Guid.Empty || account.OwnerUserId != requestingUserId)
            throw new DomainException("Domain:UsuarioNaoAutorizado");
    }
}
