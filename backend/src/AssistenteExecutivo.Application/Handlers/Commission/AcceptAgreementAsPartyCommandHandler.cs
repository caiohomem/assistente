using AssistenteExecutivo.Application.Commands.Commission;
using AssistenteExecutivo.Application.Interfaces;
using AssistenteExecutivo.Domain.Entities;
using AssistenteExecutivo.Domain.Exceptions;
using AssistenteExecutivo.Domain.Interfaces;
using MediatR;

namespace AssistenteExecutivo.Application.Handlers.Commission;

public class AcceptAgreementAsPartyCommandHandler : IRequestHandler<AcceptAgreementAsPartyCommand, Unit>
{
    private readonly ICommissionAgreementRepository _agreementRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IClock _clock;
    private readonly IPublisher _publisher;

    public AcceptAgreementAsPartyCommandHandler(
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

    public async Task<Unit> Handle(AcceptAgreementAsPartyCommand request, CancellationToken cancellationToken)
    {
        if (request.PartyId == Guid.Empty)
            throw new DomainException("Domain:PartyIdObrigatorio");
        if (request.AgreementId == Guid.Empty)
            throw new DomainException("Domain:AgreementIdObrigatorio");

        var agreement = await _agreementRepository.GetByIdAsync(request.AgreementId, cancellationToken)
            ?? throw new DomainException("Domain:AcordoNaoEncontrado");

        agreement.AcceptAgreement(request.PartyId, _clock);

        await _agreementRepository.UpdateAsync(agreement, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        await PublishDomainEventsAsync(agreement, cancellationToken);
        agreement.ClearDomainEvents();

        return Unit.Value;
    }

    private async Task PublishDomainEventsAsync(CommissionAgreement agreement, CancellationToken cancellationToken)
    {
        foreach (var domainEvent in agreement.DomainEvents)
        {
            await _publisher.Publish(domainEvent, cancellationToken);
        }
    }
}
