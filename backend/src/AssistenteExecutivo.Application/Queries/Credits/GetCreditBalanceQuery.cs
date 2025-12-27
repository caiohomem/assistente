using AssistenteExecutivo.Application.DTOs;
using MediatR;

namespace AssistenteExecutivo.Application.Queries.Credits;

public class GetCreditBalanceQuery : IRequest<CreditBalanceDto?>
{
    public Guid OwnerUserId { get; set; }
}






