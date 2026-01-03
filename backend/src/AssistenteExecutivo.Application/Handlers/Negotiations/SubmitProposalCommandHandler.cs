using AssistenteExecutivo.Application.Commands.Negotiations;
using AssistenteExecutivo.Application.Interfaces;
using AssistenteExecutivo.Domain.Entities;
using AssistenteExecutivo.Domain.Enums;
using AssistenteExecutivo.Domain.Exceptions;
using AssistenteExecutivo.Domain.Interfaces;
using MediatR;

namespace AssistenteExecutivo.Application.Handlers.Negotiations;

public class SubmitProposalCommandHandler : IRequestHandler<SubmitProposalCommand, Guid>
{
    private readonly INegotiationSessionRepository _sessionRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IClock _clock;
    private readonly IPublisher _publisher;

    public SubmitProposalCommandHandler(
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

    public async Task<Guid> Handle(SubmitProposalCommand request, CancellationToken cancellationToken)
    {
        var session = await _sessionRepository.GetByIdAsync(request.SessionId, cancellationToken)
            ?? throw new DomainException("Domain:SessaoNaoEncontrada");

        var proposalId = request.ProposalId == Guid.Empty ? Guid.NewGuid() : request.ProposalId;
        session.SubmitProposal(
            proposalId,
            request.PartyId,
            request.Source,
            request.Content,
            _clock);

        await _sessionRepository.UpdateAsync(session, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        await PublishDomainEventsAsync(session, cancellationToken);
        session.ClearDomainEvents();

        return proposalId;
    }

    private async Task PublishDomainEventsAsync(NegotiationSession session, CancellationToken cancellationToken)
    {
        foreach (var domainEvent in session.DomainEvents)
        {
            await _publisher.Publish(domainEvent, cancellationToken);
        }
    }
}
