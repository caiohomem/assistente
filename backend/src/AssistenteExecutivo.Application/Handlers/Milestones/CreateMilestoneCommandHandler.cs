using AssistenteExecutivo.Application.Commands.Milestones;
using AssistenteExecutivo.Application.Interfaces;
using AssistenteExecutivo.Domain.Entities;
using AssistenteExecutivo.Domain.Exceptions;
using AssistenteExecutivo.Domain.Interfaces;
using AssistenteExecutivo.Domain.ValueObjects;
using MediatR;

namespace AssistenteExecutivo.Application.Handlers.Milestones;

public class CreateMilestoneCommandHandler : IRequestHandler<CreateMilestoneCommand, Guid>
{
    private readonly ICommissionAgreementRepository _agreementRepository;
    private readonly IApplicationDbContext _dbContext;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IClock _clock;
    private readonly IPublisher _publisher;

    public CreateMilestoneCommandHandler(
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

    public async Task<Guid> Handle(CreateMilestoneCommand request, CancellationToken cancellationToken)
    {
        if (request.RequestedBy == Guid.Empty)
            throw new DomainException("Domain:UsuarioSolicitanteObrigatorio");
        if (request.Value <= 0)
            throw new DomainException("Domain:ValorMilestoneObrigatorio");

        var agreement = await _agreementRepository.GetByIdAsync(request.AgreementId, cancellationToken)
            ?? throw new DomainException("Domain:AcordoNaoEncontrado");

        EnsureOwner(agreement, request.RequestedBy);

        var milestoneId = request.MilestoneId == Guid.Empty ? Guid.NewGuid() : request.MilestoneId;
        var value = Money.Create(
            request.Value,
            string.IsNullOrWhiteSpace(request.Currency) ? agreement.TotalValue.Currency : request.Currency!);

        var milestone = agreement.AddMilestone(
            milestoneId,
            request.Description,
            value,
            request.DueDate,
            _clock);

        // Explicitly add the new milestone to the DbContext to ensure it's tracked as Added
        _dbContext.Milestones.Add(milestone);

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        await PublishDomainEventsAsync(agreement, cancellationToken);
        agreement.ClearDomainEvents();

        return milestone.MilestoneId;
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
