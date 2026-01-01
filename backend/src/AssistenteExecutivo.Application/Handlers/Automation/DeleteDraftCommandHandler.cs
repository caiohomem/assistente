using AssistenteExecutivo.Application.Commands.Automation;
using AssistenteExecutivo.Application.Interfaces;
using AssistenteExecutivo.Domain.Exceptions;
using MediatR;

namespace AssistenteExecutivo.Application.Handlers.Automation;

public class DeleteDraftCommandHandler : IRequestHandler<DeleteDraftCommand>
{
    private readonly IDraftDocumentRepository _draftRepository;
    private readonly IUnitOfWork _unitOfWork;

    public DeleteDraftCommandHandler(
        IDraftDocumentRepository draftRepository,
        IUnitOfWork unitOfWork)
    {
        _draftRepository = draftRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task Handle(DeleteDraftCommand request, CancellationToken cancellationToken)
    {
        if (request.DraftId == Guid.Empty)
            throw new DomainException("Domain:DraftIdObrigatorio");

        if (request.OwnerUserId == Guid.Empty)
            throw new DomainException("Domain:OwnerUserIdObrigatorio");

        var draft = await _draftRepository.GetByIdAsync(request.DraftId, request.OwnerUserId, cancellationToken);
        if (draft == null)
            throw new DomainException("Domain:DraftNaoEncontrado");

        await _draftRepository.DeleteAsync(draft, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }
}







