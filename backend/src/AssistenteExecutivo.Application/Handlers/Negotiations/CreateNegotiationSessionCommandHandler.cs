using AssistenteExecutivo.Application.Commands.Negotiations;
using AssistenteExecutivo.Application.Interfaces;
using AssistenteExecutivo.Domain.Entities;
using AssistenteExecutivo.Domain.Exceptions;
using AssistenteExecutivo.Domain.Interfaces;
using MediatR;

namespace AssistenteExecutivo.Application.Handlers.Negotiations;

public class CreateNegotiationSessionCommandHandler : IRequestHandler<CreateNegotiationSessionCommand, Guid>
{
    private readonly INegotiationSessionRepository _sessionRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IClock _clock;
    private readonly IPublisher _publisher;

    public CreateNegotiationSessionCommandHandler(
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

    public async Task<Guid> Handle(CreateNegotiationSessionCommand request, CancellationToken cancellationToken)
    {
        if (request.OwnerUserId == Guid.Empty)
            throw new DomainException("Domain:OwnerUserIdObrigatorio");

        var sessionId = request.SessionId == Guid.Empty ? Guid.NewGuid() : request.SessionId;
        var session = NegotiationSession.Create(
            sessionId,
            request.OwnerUserId,
            request.Title,
            request.Context,
            _clock);

        await _sessionRepository.AddAsync(session, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        await PublishDomainEventsAsync(session, cancellationToken);
        session.ClearDomainEvents();

        return session.SessionId;
    }

    private async Task PublishDomainEventsAsync(NegotiationSession session, CancellationToken cancellationToken)
    {
        foreach (var domainEvent in session.DomainEvents)
        {
            await _publisher.Publish(domainEvent, cancellationToken);
        }
    }
}
