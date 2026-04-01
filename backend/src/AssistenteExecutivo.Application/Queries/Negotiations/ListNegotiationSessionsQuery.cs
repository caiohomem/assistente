using AssistenteExecutivo.Application.DTOs;
using AssistenteExecutivo.Domain.Enums;
using MediatR;

namespace AssistenteExecutivo.Application.Queries.Negotiations;

public class ListNegotiationSessionsQuery : IRequest<List<NegotiationSessionDto>>
{
    public Guid OwnerUserId { get; set; }
    public NegotiationStatus? Status { get; set; }
}
