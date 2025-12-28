using AssistenteExecutivo.Application.Commands.Automation;
using AssistenteExecutivo.Application.Interfaces;
using AssistenteExecutivo.Domain.Entities;
using AssistenteExecutivo.Domain.Exceptions;
using AssistenteExecutivo.Domain.Interfaces;
using MediatR;

namespace AssistenteExecutivo.Application.Handlers.Automation;

public class CreateReminderCommandHandler : IRequestHandler<CreateReminderCommand, Guid>
{
    private readonly IReminderRepository _reminderRepository;
    private readonly IContactRepository _contactRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IClock _clock;
    private readonly IPublisher _publisher;

    public CreateReminderCommandHandler(
        IReminderRepository reminderRepository,
        IContactRepository contactRepository,
        IUnitOfWork unitOfWork,
        IClock clock,
        IPublisher publisher)
    {
        _reminderRepository = reminderRepository;
        _contactRepository = contactRepository;
        _unitOfWork = unitOfWork;
        _clock = clock;
        _publisher = publisher;
    }

    public async Task<Guid> Handle(CreateReminderCommand request, CancellationToken cancellationToken)
    {
        if (request.OwnerUserId == Guid.Empty)
            throw new DomainException("Domain:OwnerUserIdObrigatorio");

        if (request.ContactId == Guid.Empty)
            throw new DomainException("Domain:ContactIdObrigatorio");

        // Verificar se o contato existe e pertence ao usuário
        var contact = await _contactRepository.GetByIdAsync(request.ContactId, request.OwnerUserId, cancellationToken);
        if (contact == null)
            throw new DomainException("Domain:ContactNaoEncontrado");

        var reminderId = Guid.NewGuid();
        var reminder = Reminder.Create(
            reminderId,
            request.OwnerUserId,
            request.ContactId,
            request.Reason,
            request.ScheduledFor,
            _clock);

        if (!string.IsNullOrWhiteSpace(request.SuggestedMessage))
        {
            reminder.UpdateSuggestedMessage(request.SuggestedMessage, _clock);
        }

        await _reminderRepository.AddAsync(reminder, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        // Publish domain events
        foreach (var domainEvent in reminder.DomainEvents)
        {
            await _publisher.Publish(domainEvent, cancellationToken);
        }
        reminder.ClearDomainEvents();

        return reminderId;
    }
}





