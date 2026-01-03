using System.Linq;
using System.Text.Json;
using AssistenteExecutivo.Application.Commands.Negotiations;
using AssistenteExecutivo.Application.Interfaces;
using AssistenteExecutivo.Domain.Entities;
using AssistenteExecutivo.Domain.Enums;
using AssistenteExecutivo.Domain.Exceptions;
using AssistenteExecutivo.Domain.Interfaces;
using MediatR;

namespace AssistenteExecutivo.Application.Handlers.Negotiations;

public class RequestAIProposalCommandHandler : IRequestHandler<RequestAIProposalCommand, Guid>
{
    private readonly INegotiationSessionRepository _sessionRepository;
    private readonly INegotiationAIService _aiService;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IClock _clock;
    private readonly IPublisher _publisher;

    public RequestAIProposalCommandHandler(
        INegotiationSessionRepository sessionRepository,
        INegotiationAIService aiService,
        IUnitOfWork unitOfWork,
        IClock clock,
        IPublisher publisher)
    {
        _sessionRepository = sessionRepository;
        _aiService = aiService;
        _unitOfWork = unitOfWork;
        _clock = clock;
        _publisher = publisher;
    }

    public async Task<Guid> Handle(RequestAIProposalCommand request, CancellationToken cancellationToken)
    {
        if (request.RequestedBy == Guid.Empty)
            throw new DomainException("Domain:UsuarioSolicitanteObrigatorio");

        var session = await _sessionRepository.GetByIdAsync(request.SessionId, cancellationToken)
            ?? throw new DomainException("Domain:SessaoNaoEncontrada");

        if (session.OwnerUserId != request.RequestedBy)
            throw new DomainException("Domain:ApenasDonoPodeSolicitarAI");

        session.RequestAiSuggestion(request.AdditionalInstructions, _clock);

        var snapshots = session.Proposals
            .Select(p => new NegotiationProposalSnapshot
            {
                ProposalId = p.ProposalId,
                PartyId = p.PartyId,
                Source = p.Source,
                Status = p.Status,
                Content = p.Content
            })
            .ToList();

        var context = session.Context ?? string.Empty;
        var suggestion = await _aiService.SuggestIntermediateTermsAsync(context, snapshots, cancellationToken);

        var payload = new
        {
            summary = suggestion.Summary,
            terms = suggestion.SuggestedTermsJson,
            instructions = request.AdditionalInstructions,
            generatedAt = _clock.UtcNow
        };

        var content = JsonSerializer.Serialize(payload, new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            WriteIndented = false
        });

        var proposalId = Guid.NewGuid();
        session.SubmitProposal(
            proposalId,
            null,
            ProposalSource.AI,
            content,
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
