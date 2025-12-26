using AssistenteExecutivo.Application.DTOs;
using AssistenteExecutivo.Domain.Enums;
using MediatR;

namespace AssistenteExecutivo.Application.Queries.Credits;

public class ListCreditTransactionsQuery : IRequest<List<CreditTransactionDto>>
{
    public Guid OwnerUserId { get; set; }
    public CreditTransactionType? Type { get; set; }
    public DateTime? FromDate { get; set; }
    public DateTime? ToDate { get; set; }
    public int? Limit { get; set; }
    public int? Offset { get; set; }
}



