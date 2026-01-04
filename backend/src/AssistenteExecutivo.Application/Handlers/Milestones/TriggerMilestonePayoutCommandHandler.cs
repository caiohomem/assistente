using System.Linq;
using AssistenteExecutivo.Application.Commands.Milestones;
using AssistenteExecutivo.Application.Interfaces;
using AssistenteExecutivo.Domain.DomainServices;
using AssistenteExecutivo.Domain.Entities;
using AssistenteExecutivo.Domain.Enums;
using AssistenteExecutivo.Domain.Exceptions;
using AssistenteExecutivo.Domain.Interfaces;
using AssistenteExecutivo.Domain.ValueObjects;
using Microsoft.EntityFrameworkCore;
using MediatR;

namespace AssistenteExecutivo.Application.Handlers.Milestones;

public class TriggerMilestonePayoutCommandHandler : IRequestHandler<TriggerMilestonePayoutCommand, Guid>
{
    private readonly ICommissionAgreementRepository _agreementRepository;
    private readonly IEscrowAccountRepository _escrowAccountRepository;
    private readonly EscrowPayoutDomainService _domainService;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IClock _clock;
    private readonly IPublisher _publisher;

    public TriggerMilestonePayoutCommandHandler(
        ICommissionAgreementRepository agreementRepository,
        IEscrowAccountRepository escrowAccountRepository,
        EscrowPayoutDomainService domainService,
        IUnitOfWork unitOfWork,
        IClock clock,
        IPublisher publisher)
    {
        _agreementRepository = agreementRepository;
        _escrowAccountRepository = escrowAccountRepository;
        _domainService = domainService;
        _unitOfWork = unitOfWork;
        _clock = clock;
        _publisher = publisher;
    }

    public async Task<Guid> Handle(TriggerMilestonePayoutCommand request, CancellationToken cancellationToken)
    {
        if (request.RequestedBy == Guid.Empty)
            throw new DomainException("Domain:UsuarioSolicitanteObrigatorio");

        var agreement = await _agreementRepository.GetByIdAsync(request.AgreementId, cancellationToken)
            ?? throw new DomainException("Domain:AcordoNaoEncontrado");

        EnsureOwner(agreement, request.RequestedBy);

        var milestone = agreement.Milestones.FirstOrDefault(m => m.MilestoneId == request.MilestoneId)
            ?? throw new DomainException("Domain:MilestoneNaoEncontrado");

        if (!agreement.EscrowAccountId.HasValue)
            throw new DomainException("Domain:AcordoNaoPossuiEscrow");

        var escrowAccount = await _escrowAccountRepository.GetByIdAsync(agreement.EscrowAccountId.Value, cancellationToken)
            ?? throw new DomainException("Domain:ContaEscrowNaoEncontrada");

        var amountValue = request.Amount > 0 ? request.Amount : milestone.Value.Amount;
        var amount = Money.Create(amountValue, string.IsNullOrWhiteSpace(request.Currency) ? milestone.Value.Currency : request.Currency!);

        _domainService.EnsureMilestoneEligibleForPayout(agreement, milestone, amount);
        _domainService.EnsureEscrowCoverage(escrowAccount, amount);

        var approvalType = _domainService.DetermineApprovalPolicy(agreement, amount);
        var transactionId = Guid.NewGuid();

        var transaction = escrowAccount.RequestPayout(
            transactionId,
            request.BeneficiaryPartyId,
            amount,
            $"Payout milestone {milestone.Description}",
            approvalType,
            null,
            _clock);

        agreement.CompleteMilestone(milestone.MilestoneId, "Payout solicitado", transaction.TransactionId, _clock);

        // Explicitly add the new transaction to the context
        await _escrowAccountRepository.AddTransactionAsync(transaction, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        await PublishEventsAsync(escrowAccount, agreement, cancellationToken);

        escrowAccount.ClearDomainEvents();
        agreement.ClearDomainEvents();

        return transaction.TransactionId;
    }

    private static void EnsureOwner(CommissionAgreement agreement, Guid userId)
    {
        if (agreement.OwnerUserId != userId)
            throw new DomainException("Domain:ApenasDonoPodeModificarAcordo");
    }

    private async Task PublishEventsAsync(EscrowAccount escrowAccount, CommissionAgreement agreement, CancellationToken cancellationToken)
    {
        foreach (var domainEvent in escrowAccount.DomainEvents)
        {
            await _publisher.Publish(domainEvent, cancellationToken);
        }

        foreach (var domainEvent in agreement.DomainEvents)
        {
            await _publisher.Publish(domainEvent, cancellationToken);
        }
    }
}
