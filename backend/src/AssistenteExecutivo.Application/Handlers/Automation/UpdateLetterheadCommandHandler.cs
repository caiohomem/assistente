using AssistenteExecutivo.Application.Commands.Automation;
using AssistenteExecutivo.Application.Interfaces;
using AssistenteExecutivo.Domain.Exceptions;
using AssistenteExecutivo.Domain.Interfaces;
using MediatR;

namespace AssistenteExecutivo.Application.Handlers.Automation;

public class UpdateLetterheadCommandHandler : IRequestHandler<UpdateLetterheadCommand>
{
    private readonly ILetterheadRepository _letterheadRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IClock _clock;

    public UpdateLetterheadCommandHandler(
        ILetterheadRepository letterheadRepository,
        IUnitOfWork unitOfWork,
        IClock clock)
    {
        _letterheadRepository = letterheadRepository;
        _unitOfWork = unitOfWork;
        _clock = clock;
    }

    public async Task Handle(UpdateLetterheadCommand request, CancellationToken cancellationToken)
    {
        var letterhead = await _letterheadRepository.GetByIdAsync(request.LetterheadId, request.OwnerUserId, cancellationToken);
        if (letterhead == null)
            throw new DomainException("Domain:LetterheadNaoEncontrado");

        if (!string.IsNullOrWhiteSpace(request.Name))
        {
            letterhead.UpdateName(request.Name, _clock);
        }

        if (!string.IsNullOrWhiteSpace(request.DesignData))
        {
            letterhead.UpdateDesignData(request.DesignData, _clock);
        }

        if (request.IsActive.HasValue)
        {
            if (request.IsActive.Value)
                letterhead.Activate(_clock);
            else
                letterhead.Deactivate(_clock);
        }

        await _letterheadRepository.UpdateAsync(letterhead, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }
}







