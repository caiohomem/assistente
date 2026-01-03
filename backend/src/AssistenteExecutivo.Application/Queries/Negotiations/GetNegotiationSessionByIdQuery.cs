using AssistenteExecutivo.Application.DTOs;
using MediatR;

namespace AssistenteExecutivo.Application.Queries.Negotiations;

public class GetNegotiationSessionByIdQuery : IRequest<NegotiationSessionDto?>
{
    public Guid SessionId { get; set; }
    public Guid RequestingUserId { get; set; }
}
