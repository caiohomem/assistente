using AssistenteExecutivo.Application.Commands.Automation;
using AssistenteExecutivo.Application.Interfaces;
using AssistenteExecutivo.Domain.Exceptions;
using AssistenteExecutivo.Domain.Interfaces;
using MediatR;

namespace AssistenteExecutivo.Application.Handlers.Automation;

public class ApproveDraftCommandHandler : IRequestHandler<ApproveDraftCommand>
{
    private readonly IDraftDocumentRepository _draftRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IClock _clock;
    private readonly IPublisher _publisher;

    public ApproveDraftCommandHandler(
        IDraftDocumentRepository draftRepository,
        IUnitOfWork unitOfWork,
        IClock clock,
        IPublisher publisher)
    {
        _draftRepository = draftRepository;
        _unitOfWork = unitOfWork;
        _clock = clock;
        _publisher = publisher;
    }

    public async Task Handle(ApproveDraftCommand request, CancellationToken cancellationToken)
    {
        var draft = await _draftRepository.GetByIdAsync(request.DraftId, request.OwnerUserId, cancellationToken);
        if (draft == null)
            throw new DomainException("Domain:DraftNaoEncontrado");

        draft.Approve(request.OwnerUserId, _clock);

        await _draftRepository.UpdateAsync(draft, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        // Publish domain events
        foreach (var domainEvent in draft.DomainEvents)
        {
            await _publisher.Publish(domainEvent, cancellationToken);
        }
        draft.ClearDomainEvents();
    }
}





