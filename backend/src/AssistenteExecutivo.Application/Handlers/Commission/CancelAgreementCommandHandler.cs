using AssistenteExecutivo.Application.Commands.Commission;
using AssistenteExecutivo.Application.Interfaces;
using AssistenteExecutivo.Domain.Entities;
using AssistenteExecutivo.Domain.Exceptions;
using AssistenteExecutivo.Domain.Interfaces;
using MediatR;

namespace AssistenteExecutivo.Application.Handlers.Commission;

public class CancelAgreementCommandHandler : IRequestHandler<CancelAgreementCommand, Unit>
{
    private readonly ICommissionAgreementRepository _agreementRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IClock _clock;
    private readonly IPublisher _publisher;

    public CancelAgreementCommandHandler(
        ICommissionAgreementRepository agreementRepository,
        IUnitOfWork unitOfWork,
        IClock clock,
        IPublisher publisher)
    {
        _agreementRepository = agreementRepository;
        _unitOfWork = unitOfWork;
        _clock = clock;
        _publisher = publisher;
    }

    public async Task<Unit> Handle(CancelAgreementCommand request, CancellationToken cancellationToken)
    {
        if (request.AgreementId == Guid.Empty)
            throw new DomainException("Domain:AgreementIdObrigatorio");
        if (request.RequestedBy == Guid.Empty)
            throw new DomainException("Domain:UsuarioSolicitanteObrigatorio");

        var agreement = await _agreementRepository.GetByIdAsync(request.AgreementId, cancellationToken)
            ?? throw new DomainException("Domain:AcordoNaoEncontrado");

        EnsureOwner(agreement, request.RequestedBy);

        agreement.Cancel(request.Reason, _clock);

        await _agreementRepository.UpdateAsync(agreement, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        await PublishDomainEventsAsync(agreement, cancellationToken);
        agreement.ClearDomainEvents();

        return Unit.Value;
    }

    private static void EnsureOwner(CommissionAgreement agreement, Guid userId)
    {
        if (agreement.OwnerUserId != userId)
            throw new DomainException("Domain:UsuarioNaoAutorizado");
    }

    private async Task PublishDomainEventsAsync(CommissionAgreement agreement, CancellationToken cancellationToken)
    {
        foreach (var domainEvent in agreement.DomainEvents)
        {
            await _publisher.Publish(domainEvent, cancellationToken);
        }
    }
}
