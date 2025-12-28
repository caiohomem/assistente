using AssistenteExecutivo.Application.DTOs;
using AssistenteExecutivo.Application.Interfaces;
using AssistenteExecutivo.Application.Queries.Contacts;
using MediatR;

namespace AssistenteExecutivo.Application.Handlers.Contacts;

public class GetNetworkGraphQueryHandler : IRequestHandler<GetNetworkGraphQuery, NetworkGraphDto>
{
    private readonly IContactRepository _contactRepository;

    public GetNetworkGraphQueryHandler(IContactRepository contactRepository)
    {
        _contactRepository = contactRepository;
    }

    public async Task<NetworkGraphDto> Handle(GetNetworkGraphQuery request, CancellationToken cancellationToken)
    {
        // Validar maxDepth
        var maxDepth = Math.Max(1, Math.Min(5, request.MaxDepth)); // Limitar entre 1 e 5

        var graph = await _contactRepository.GetNetworkGraphAsync(
            request.OwnerUserId,
            maxDepth,
            cancellationToken);

        return graph;
    }
}

