using AssistenteExecutivo.Application.Commands.Commission;
using AssistenteExecutivo.Application.Interfaces;
using AssistenteExecutivo.Domain.DomainServices;
using AssistenteExecutivo.Domain.Entities;
using AssistenteExecutivo.Domain.Exceptions;
using AssistenteExecutivo.Domain.Interfaces;
using MediatR;

namespace AssistenteExecutivo.Application.Handlers.Commission;

public class CompleteAgreementCommandHandler : IRequestHandler<CompleteAgreementCommand, Unit>
{
    private readonly ICommissionAgreementRepository _agreementRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly CommissionAgreementRulesService _rulesService;
    private readonly IClock _clock;
    private readonly IPublisher _publisher;

    public CompleteAgreementCommandHandler(
        ICommissionAgreementRepository agreementRepository,
        IUnitOfWork unitOfWork,
        CommissionAgreementRulesService rulesService,
        IClock clock,
        IPublisher publisher)
    {
        _agreementRepository = agreementRepository;
        _unitOfWork = unitOfWork;
        _rulesService = rulesService;
        _clock = clock;
        _publisher = publisher;
    }

    public async Task<Unit> Handle(CompleteAgreementCommand request, CancellationToken cancellationToken)
    {
        if (request.AgreementId == Guid.Empty)
            throw new DomainException("Domain:AgreementIdObrigatorio");
        if (request.RequestedBy == Guid.Empty)
            throw new DomainException("Domain:UsuarioSolicitanteObrigatorio");

        var agreement = await _agreementRepository.GetByIdAsync(request.AgreementId, cancellationToken)
            ?? throw new DomainException("Domain:AcordoNaoEncontrado");

        EnsureOwner(agreement, request.RequestedBy);

        _rulesService.EnsureCanComplete(agreement);
        agreement.Complete(_clock);

        await _agreementRepository.UpdateAsync(agreement, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        await PublishDomainEventsAsync(agreement, cancellationToken);
        agreement.ClearDomainEvents();

        return Unit.Value;
    }

    private static void EnsureOwner(CommissionAgreement agreement, Guid userId)
    {
        if (agreement.OwnerUserId != userId)
            throw new DomainException("Domain:ApenasDonoPodeModificarAcordo");
    }

    private async Task PublishDomainEventsAsync(CommissionAgreement agreement, CancellationToken cancellationToken)
    {
        foreach (var domainEvent in agreement.DomainEvents)
        {
            await _publisher.Publish(domainEvent, cancellationToken);
        }
    }
}
