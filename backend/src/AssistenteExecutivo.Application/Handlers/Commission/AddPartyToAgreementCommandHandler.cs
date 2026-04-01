using AssistenteExecutivo.Application.Commands.Commission;
using AssistenteExecutivo.Application.Interfaces;
using AssistenteExecutivo.Domain.Entities;
using AssistenteExecutivo.Domain.Enums;
using AssistenteExecutivo.Domain.Exceptions;
using AssistenteExecutivo.Domain.Interfaces;
using AssistenteExecutivo.Domain.ValueObjects;
using MediatR;

namespace AssistenteExecutivo.Application.Handlers.Commission;

public class AddPartyToAgreementCommandHandler : IRequestHandler<AddPartyToAgreementCommand, Unit>
{
    private readonly ICommissionAgreementRepository _agreementRepository;
    private readonly IApplicationDbContext _dbContext;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IClock _clock;
    private readonly IPublisher _publisher;

    public AddPartyToAgreementCommandHandler(
        ICommissionAgreementRepository agreementRepository,
        IApplicationDbContext dbContext,
        IUnitOfWork unitOfWork,
        IClock clock,
        IPublisher publisher)
    {
        _agreementRepository = agreementRepository;
        _dbContext = dbContext;
        _unitOfWork = unitOfWork;
        _clock = clock;
        _publisher = publisher;
    }

    public async Task<Unit> Handle(AddPartyToAgreementCommand request, CancellationToken cancellationToken)
    {
        if (request.RequestedBy == Guid.Empty)
            throw new DomainException("Domain:UsuarioSolicitanteObrigatorio");

        var agreement = await _agreementRepository.GetByIdAsync(request.AgreementId, cancellationToken)
            ?? throw new DomainException("Domain:AcordoNaoEncontrado");

        EnsureOwner(agreement, request.RequestedBy);

        var split = Percentage.Create(request.SplitPercentage);
        var party = agreement.AddParty(
            request.PartyId == Guid.Empty ? Guid.NewGuid() : request.PartyId,
            request.ContactId,
            request.CompanyId,
            request.PartyName,
            request.Email,
            split,
            request.Role,
            request.StripeAccountId,
            _clock);

        // Explicitly add the new party to the DbContext to ensure it's tracked as Added
        _dbContext.AgreementParties.Add(party);

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
