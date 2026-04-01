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
    private readonly IApplicationDbContext _dbContext;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IClock _clock;
    private readonly IPublisher _publisher;

    public SubmitProposalCommandHandler(
        INegotiationSessionRepository sessionRepository,
        IApplicationDbContext dbContext,
        IUnitOfWork unitOfWork,
        IClock clock,
        IPublisher publisher)
    {
        _sessionRepository = sessionRepository;
        _dbContext = dbContext;
        _unitOfWork = unitOfWork;
        _clock = clock;
        _publisher = publisher;
    }

    public async Task<Guid> Handle(SubmitProposalCommand request, CancellationToken cancellationToken)
    {
        var session = await _sessionRepository.GetByIdAsync(request.SessionId, cancellationToken)
            ?? throw new DomainException("Domain:SessaoNaoEncontrada");

        var proposalId = request.ProposalId == Guid.Empty ? Guid.NewGuid() : request.ProposalId;
        var proposal = session.SubmitProposal(
            proposalId,
            request.PartyId,
            request.Source,
            request.Content,
            _clock);

        // Explicitly add the new proposal to the DbContext to ensure it's tracked as Added
        _dbContext.NegotiationProposals.Add(proposal);

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
