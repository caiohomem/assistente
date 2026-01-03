using AssistenteExecutivo.Application.Commands.Escrow;
using AssistenteExecutivo.Application.Interfaces;
using AssistenteExecutivo.Domain.DomainServices;
using AssistenteExecutivo.Domain.Entities;
using AssistenteExecutivo.Domain.Enums;
using AssistenteExecutivo.Domain.Exceptions;
using AssistenteExecutivo.Domain.Interfaces;
using AssistenteExecutivo.Domain.ValueObjects;
using MediatR;

namespace AssistenteExecutivo.Application.Handlers.Escrow;

public class RequestPayoutCommandHandler : IRequestHandler<RequestPayoutCommand, Guid>
{
    private readonly IEscrowAccountRepository _escrowAccountRepository;
    private readonly ICommissionAgreementRepository _agreementRepository;
    private readonly EscrowPayoutDomainService _domainService;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IClock _clock;
    private readonly IPublisher _publisher;

    public RequestPayoutCommandHandler(
        IEscrowAccountRepository escrowAccountRepository,
        ICommissionAgreementRepository agreementRepository,
        EscrowPayoutDomainService domainService,
        IUnitOfWork unitOfWork,
        IClock clock,
        IPublisher publisher)
    {
        _escrowAccountRepository = escrowAccountRepository;
        _agreementRepository = agreementRepository;
        _domainService = domainService;
        _unitOfWork = unitOfWork;
        _clock = clock;
        _publisher = publisher;
    }

    public async Task<Guid> Handle(RequestPayoutCommand request, CancellationToken cancellationToken)
    {
        if (request.RequestedBy == Guid.Empty)
            throw new DomainException("Domain:UsuarioSolicitanteObrigatorio");
        if (request.Amount <= 0)
            throw new DomainException("Domain:ValorPayoutObrigatorio");

        var account = await _escrowAccountRepository.GetByIdAsync(request.EscrowAccountId, cancellationToken)
            ?? throw new DomainException("Domain:ContaEscrowNaoEncontrada");
        EnsureOwner(account, request.RequestedBy);

        var agreement = await _agreementRepository.GetByIdAsync(account.AgreementId, cancellationToken)
            ?? throw new DomainException("Domain:AcordoNaoEncontrado");

        var amount = Money.Create(request.Amount, string.IsNullOrWhiteSpace(request.Currency) ? account.Currency : request.Currency!);
        _domainService.EnsureEscrowCoverage(account, amount);

        var approvalType = _domainService.DetermineApprovalPolicy(agreement, amount);
        var transactionId = request.TransactionId == Guid.Empty ? Guid.NewGuid() : request.TransactionId;

        var transaction = account.RequestPayout(
            transactionId,
            request.PartyId,
            amount,
            request.Description,
            approvalType,
            request.IdempotencyKey,
            _clock);

        await _escrowAccountRepository.UpdateAsync(account, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        await PublishDomainEventsAsync(account, cancellationToken);
        account.ClearDomainEvents();

        return transaction.TransactionId;
    }

    private static void EnsureOwner(EscrowAccount account, Guid userId)
    {
        if (account.OwnerUserId != userId)
            throw new DomainException("Domain:UsuarioNaoAutorizado");
    }

    private async Task PublishDomainEventsAsync(EscrowAccount account, CancellationToken cancellationToken)
    {
        foreach (var domainEvent in account.DomainEvents)
        {
            await _publisher.Publish(domainEvent, cancellationToken);
        }
    }
}
