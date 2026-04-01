using AssistenteExecutivo.Application.Commands.Negotiations;
using AssistenteExecutivo.Application.Interfaces;
using AssistenteExecutivo.Domain.Entities;
using AssistenteExecutivo.Domain.Exceptions;
using AssistenteExecutivo.Domain.Interfaces;
using MediatR;

namespace AssistenteExecutivo.Application.Handlers.Negotiations;

public class GenerateAgreementFromNegotiationCommandHandler : IRequestHandler<GenerateAgreementFromNegotiationCommand, Guid>
{
    private readonly INegotiationSessionRepository _sessionRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IClock _clock;
    private readonly IPublisher _publisher;

    public GenerateAgreementFromNegotiationCommandHandler(
        INegotiationSessionRepository sessionRepository,
        IUnitOfWork unitOfWork,
        IClock clock,
        IPublisher publisher)
    {
        _sessionRepository = sessionRepository;
        _unitOfWork = unitOfWork;
        _clock = clock;
        _publisher = publisher;
    }

    public async Task<Guid> Handle(GenerateAgreementFromNegotiationCommand request, CancellationToken cancellationToken)
    {
        if (request.AgreementId == Guid.Empty)
            throw new DomainException("Domain:AgreementIdObrigatorio");

        var session = await _sessionRepository.GetByIdAsync(request.SessionId, cancellationToken)
            ?? throw new DomainException("Domain:SessaoNaoEncontrada");

        if (session.OwnerUserId != request.OwnerUserId)
            throw new DomainException("Domain:ApenasDonoPodeGerarAcordo");

        session.GenerateAgreement(request.AgreementId, _clock);

        await _sessionRepository.UpdateAsync(session, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        await PublishDomainEventsAsync(session, cancellationToken);
        session.ClearDomainEvents();

        return request.AgreementId;
    }

    private async Task PublishDomainEventsAsync(NegotiationSession session, CancellationToken cancellationToken)
    {
        foreach (var domainEvent in session.DomainEvents)
        {
            await _publisher.Publish(domainEvent, cancellationToken);
        }
    }
}
