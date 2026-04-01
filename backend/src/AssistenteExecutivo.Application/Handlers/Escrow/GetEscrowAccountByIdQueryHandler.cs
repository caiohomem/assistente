using AssistenteExecutivo.Application.DTOs;
using AssistenteExecutivo.Application.Interfaces;
using AssistenteExecutivo.Application.Queries.Escrow;
using AssistenteExecutivo.Domain.Exceptions;
using MediatR;

namespace AssistenteExecutivo.Application.Handlers.Escrow;

public class GetEscrowAccountByIdQueryHandler : IRequestHandler<GetEscrowAccountByIdQuery, EscrowAccountDto?>
{
    private readonly IEscrowAccountRepository _escrowAccountRepository;

    public GetEscrowAccountByIdQueryHandler(IEscrowAccountRepository escrowAccountRepository)
    {
        _escrowAccountRepository = escrowAccountRepository;
    }

    public async Task<EscrowAccountDto?> Handle(GetEscrowAccountByIdQuery request, CancellationToken cancellationToken)
    {
        if (request.EscrowAccountId == Guid.Empty)
            throw new DomainException("Domain:EscrowAccountIdObrigatorio");

        var account = await _escrowAccountRepository.GetByIdAsync(request.EscrowAccountId, cancellationToken);
        if (account == null)
            return null;

        EnsureOwner(account, request.RequestingUserId);
        return EscrowMapper.Map(account);
    }

    private static void EnsureOwner(Domain.Entities.EscrowAccount account, Guid requestingUserId)
    {
        if (requestingUserId == Guid.Empty || account.OwnerUserId != requestingUserId)
            throw new DomainException("Domain:UsuarioNaoAutorizado");
    }
}
