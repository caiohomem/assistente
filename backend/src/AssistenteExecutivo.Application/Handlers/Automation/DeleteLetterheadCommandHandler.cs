using AssistenteExecutivo.Application.Commands.Automation;
using AssistenteExecutivo.Application.Interfaces;
using AssistenteExecutivo.Domain.Exceptions;
using MediatR;

namespace AssistenteExecutivo.Application.Handlers.Automation;

public class DeleteLetterheadCommandHandler : IRequestHandler<DeleteLetterheadCommand>
{
    private readonly ILetterheadRepository _letterheadRepository;
    private readonly IUnitOfWork _unitOfWork;

    public DeleteLetterheadCommandHandler(
        ILetterheadRepository letterheadRepository,
        IUnitOfWork unitOfWork)
    {
        _letterheadRepository = letterheadRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task Handle(DeleteLetterheadCommand request, CancellationToken cancellationToken)
    {
        if (request.LetterheadId == Guid.Empty)
            throw new DomainException("Domain:LetterheadIdObrigatorio");

        if (request.OwnerUserId == Guid.Empty)
            throw new DomainException("Domain:OwnerUserIdObrigatorio");

        var letterhead = await _letterheadRepository.GetByIdAsync(request.LetterheadId, request.OwnerUserId, cancellationToken);
        if (letterhead == null)
            throw new DomainException("Domain:LetterheadNaoEncontrado");

        await _letterheadRepository.DeleteAsync(letterhead, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }
}







