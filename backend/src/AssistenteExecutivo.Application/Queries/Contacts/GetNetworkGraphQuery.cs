using AssistenteExecutivo.Application.DTOs;
using MediatR;

namespace AssistenteExecutivo.Application.Queries.Contacts;

public class GetNetworkGraphQuery : IRequest<NetworkGraphDto>
{
    public Guid OwnerUserId { get; set; }
    public int MaxDepth { get; set; } = 2;
}



