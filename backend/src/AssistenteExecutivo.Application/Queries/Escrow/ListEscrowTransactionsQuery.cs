using AssistenteExecutivo.Application.DTOs;
using MediatR;

namespace AssistenteExecutivo.Application.Queries.Escrow;

public class ListEscrowTransactionsQuery : IRequest<List<EscrowTransactionDto>>
{
    public Guid EscrowAccountId { get; set; }
    public Guid RequestingUserId { get; set; }
}
