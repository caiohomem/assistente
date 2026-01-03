using AssistenteExecutivo.Application.Commands.Escrow;
using AssistenteExecutivo.Application.Interfaces;
using AssistenteExecutivo.Domain.Exceptions;
using AssistenteExecutivo.Domain.Interfaces;
using MediatR;

namespace AssistenteExecutivo.Application.Handlers.Escrow;

public class RejectPayoutCommandHandler : IRequestHandler<RejectPayoutCommand, Unit>
{
    private readonly IEscrowAccountRepository _escrowAccountRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IClock _clock;
    private readonly IPublisher _publisher;

    public RejectPayoutCommandHandler(
        IEscrowAccountRepository escrowAccountRepository,
        IUnitOfWork unitOfWork,
        IClock clock,
        IPublisher publisher)
    {
        _escrowAccountRepository = escrowAccountRepository;
        _unitOfWork = unitOfWork;
        _clock = clock;
        _publisher = publisher;
    }

    public async Task<Unit> Handle(RejectPayoutCommand request, CancellationToken cancellationToken)
    {
        if (request.RejectedBy == Guid.Empty)
            throw new DomainException("Domain:UsuarioRejeicaoObrigatorio");
        if (string.IsNullOrWhiteSpace(request.Reason))
            throw new DomainException("Domain:MotivoRejeicaoObrigatorio");

        var account = await _escrowAccountRepository.GetByIdAsync(request.EscrowAccountId, cancellationToken)
            ?? throw new DomainException("Domain:ContaEscrowNaoEncontrada");

        EnsureOwner(account, request.RejectedBy);

        account.RejectPayout(request.TransactionId, request.RejectedBy, request.Reason, _clock);

        await _escrowAccountRepository.UpdateAsync(account, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        await PublishDomainEventsAsync(account, cancellationToken);
        account.ClearDomainEvents();

        return Unit.Value;
    }

    private static void EnsureOwner(Domain.Entities.EscrowAccount account, Guid userId)
    {
        if (account.OwnerUserId != userId)
            throw new DomainException("Domain:UsuarioNaoAutorizado");
    }

    private async Task PublishDomainEventsAsync(Domain.Entities.EscrowAccount account, CancellationToken cancellationToken)
    {
        foreach (var domainEvent in account.DomainEvents)
        {
            await _publisher.Publish(domainEvent, cancellationToken);
        }
    }
}
