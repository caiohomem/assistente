using AssistenteExecutivo.Application.DTOs;
using MediatR;

namespace AssistenteExecutivo.Application.Queries.Escrow;

public class GetEscrowAccountByIdQuery : IRequest<EscrowAccountDto?>
{
    public Guid EscrowAccountId { get; set; }
    public Guid RequestingUserId { get; set; }
}
