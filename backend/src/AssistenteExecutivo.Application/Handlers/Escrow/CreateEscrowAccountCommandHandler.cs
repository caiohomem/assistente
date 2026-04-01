using AssistenteExecutivo.Application.Commands.Escrow;
using AssistenteExecutivo.Application.Interfaces;
using AssistenteExecutivo.Domain.Entities;
using AssistenteExecutivo.Domain.Exceptions;
using AssistenteExecutivo.Domain.Interfaces;
using MediatR;

namespace AssistenteExecutivo.Application.Handlers.Escrow;

public class CreateEscrowAccountCommandHandler : IRequestHandler<CreateEscrowAccountCommand, Guid>
{
    private readonly ICommissionAgreementRepository _agreementRepository;
    private readonly IEscrowAccountRepository _escrowAccountRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IClock _clock;
    private readonly IPublisher _publisher;

    public CreateEscrowAccountCommandHandler(
        ICommissionAgreementRepository agreementRepository,
        IEscrowAccountRepository escrowAccountRepository,
        IUnitOfWork unitOfWork,
        IClock clock,
        IPublisher publisher)
    {
        _agreementRepository = agreementRepository;
        _escrowAccountRepository = escrowAccountRepository;
        _unitOfWork = unitOfWork;
        _clock = clock;
        _publisher = publisher;
    }

    public async Task<Guid> Handle(CreateEscrowAccountCommand request, CancellationToken cancellationToken)
    {
        if (request.OwnerUserId == Guid.Empty)
            throw new DomainException("Domain:OwnerUserIdObrigatorio");

        var agreement = await _agreementRepository.GetByIdAsync(request.AgreementId, cancellationToken)
            ?? throw new DomainException("Domain:AcordoNaoEncontrado");

        if (agreement.OwnerUserId != request.OwnerUserId)
            throw new DomainException("Domain:UsuarioNaoAutorizado");

        if (agreement.EscrowAccountId.HasValue)
            throw new DomainException("Domain:AcordoJaPossuiEscrow");

        var existingAccount = await _escrowAccountRepository.GetByAgreementIdAsync(request.AgreementId, cancellationToken);
        if (existingAccount != null)
            throw new DomainException("Domain:AcordoJaPossuiEscrow");

        var escrowAccountId = request.EscrowAccountId == Guid.Empty ? Guid.NewGuid() : request.EscrowAccountId;
        var account = EscrowAccount.Create(
            escrowAccountId,
            agreement.AgreementId,
            agreement.OwnerUserId,
            string.IsNullOrWhiteSpace(request.Currency) ? agreement.TotalValue.Currency : request.Currency,
            _clock);

        agreement.AttachEscrowAccount(account.EscrowAccountId);

        await _escrowAccountRepository.AddAsync(account, cancellationToken);
        await _agreementRepository.UpdateAsync(agreement, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        await PublishDomainEventsAsync(account, cancellationToken);
        account.ClearDomainEvents();

        return account.EscrowAccountId;
    }

    private async Task PublishDomainEventsAsync(EscrowAccount account, CancellationToken cancellationToken)
    {
        foreach (var domainEvent in account.DomainEvents)
        {
            await _publisher.Publish(domainEvent, cancellationToken);
        }
    }
}
