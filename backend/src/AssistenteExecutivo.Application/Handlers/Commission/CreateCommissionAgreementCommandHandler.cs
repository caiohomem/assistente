using AssistenteExecutivo.Application.Commands.Commission;
using AssistenteExecutivo.Application.Interfaces;
using AssistenteExecutivo.Domain.Entities;
using AssistenteExecutivo.Domain.Exceptions;
using AssistenteExecutivo.Domain.Interfaces;
using AssistenteExecutivo.Domain.ValueObjects;
using MediatR;

namespace AssistenteExecutivo.Application.Handlers.Commission;

public class CreateCommissionAgreementCommandHandler : IRequestHandler<CreateCommissionAgreementCommand, Guid>
{
    private readonly ICommissionAgreementRepository _agreementRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IClock _clock;
    private readonly IPublisher _publisher;

    public CreateCommissionAgreementCommandHandler(
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

    public async Task<Guid> Handle(CreateCommissionAgreementCommand request, CancellationToken cancellationToken)
    {
        if (request.OwnerUserId == Guid.Empty)
            throw new DomainException("Domain:OwnerUserIdObrigatorio");

        if (request.TotalValue <= 0)
            throw new DomainException("Domain:ValorTotalAcordoObrigatorio");

        var agreementId = request.AgreementId == Guid.Empty ? Guid.NewGuid() : request.AgreementId;
        var totalValue = Money.Create(request.TotalValue, request.Currency);

        var agreement = CommissionAgreement.Create(
            agreementId,
            request.OwnerUserId,
            request.Title,
            request.Description,
            totalValue,
            request.Terms,
            _clock);

        await _agreementRepository.AddAsync(agreement, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        await PublishDomainEventsAsync(agreement, cancellationToken);
        agreement.ClearDomainEvents();

        return agreement.AgreementId;
    }

    private async Task PublishDomainEventsAsync(CommissionAgreement agreement, CancellationToken cancellationToken)
    {
        foreach (var domainEvent in agreement.DomainEvents)
        {
            await _publisher.Publish(domainEvent, cancellationToken);
        }
    }
}
