using AssistenteExecutivo.Application.Commands.Automation;
using AssistenteExecutivo.Application.Interfaces;
using AssistenteExecutivo.Domain.Entities;
using AssistenteExecutivo.Domain.Exceptions;
using AssistenteExecutivo.Domain.Interfaces;
using MediatR;

namespace AssistenteExecutivo.Application.Handlers.Automation;

public class CreateLetterheadCommandHandler : IRequestHandler<CreateLetterheadCommand, Guid>
{
    private readonly ILetterheadRepository _letterheadRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IClock _clock;
    private readonly IPublisher _publisher;

    public CreateLetterheadCommandHandler(
        ILetterheadRepository letterheadRepository,
        IUnitOfWork unitOfWork,
        IClock clock,
        IPublisher publisher)
    {
        _letterheadRepository = letterheadRepository;
        _unitOfWork = unitOfWork;
        _clock = clock;
        _publisher = publisher;
    }

    public async Task<Guid> Handle(CreateLetterheadCommand request, CancellationToken cancellationToken)
    {
        if (request.OwnerUserId == Guid.Empty)
            throw new DomainException("Domain:OwnerUserIdObrigatorio");

        var letterheadId = Guid.NewGuid();
        var letterhead = Letterhead.Create(
            letterheadId,
            request.OwnerUserId,
            request.Name,
            request.DesignData,
            _clock);

        await _letterheadRepository.AddAsync(letterhead, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        // Publish domain events
        foreach (var domainEvent in letterhead.DomainEvents)
        {
            await _publisher.Publish(domainEvent, cancellationToken);
        }
        letterhead.ClearDomainEvents();

        return letterheadId;
    }
}







