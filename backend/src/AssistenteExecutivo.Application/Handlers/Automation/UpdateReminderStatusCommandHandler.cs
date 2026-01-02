using AssistenteExecutivo.Application.Commands.Automation;
using AssistenteExecutivo.Application.Interfaces;
using AssistenteExecutivo.Domain.Enums;
using AssistenteExecutivo.Domain.Exceptions;
using AssistenteExecutivo.Domain.Interfaces;
using MediatR;

namespace AssistenteExecutivo.Application.Handlers.Automation;

public class UpdateReminderStatusCommandHandler : IRequestHandler<UpdateReminderStatusCommand>
{
    private readonly IReminderRepository _reminderRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IClock _clock;
    private readonly IPublisher _publisher;

    public UpdateReminderStatusCommandHandler(
        IReminderRepository reminderRepository,
        IUnitOfWork unitOfWork,
        IClock clock,
        IPublisher publisher)
    {
        _reminderRepository = reminderRepository;
        _unitOfWork = unitOfWork;
        _clock = clock;
        _publisher = publisher;
    }

    public async Task Handle(UpdateReminderStatusCommand request, CancellationToken cancellationToken)
    {
        var reminder = await _reminderRepository.GetByIdAsync(request.ReminderId, request.OwnerUserId, cancellationToken);
        if (reminder == null)
            throw new DomainException("Domain:ReminderNaoEncontrado");

        switch (request.NewStatus)
        {
            case ReminderStatus.Sent:
                reminder.MarkAsSent(_clock);
                break;
            case ReminderStatus.Dismissed:
                reminder.Dismiss(_clock);
                break;
            case ReminderStatus.Snoozed:
                if (!request.NewScheduledFor.HasValue)
                    throw new DomainException("Domain:NewScheduledForObrigatorioParaSnoozed");
                reminder.Snooze(request.NewScheduledFor.Value, _clock);
                break;
            default:
                throw new DomainException("Domain:ReminderStatusInvalido");
        }

        await _reminderRepository.UpdateAsync(reminder, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        // Publish domain events
        foreach (var domainEvent in reminder.DomainEvents)
        {
            await _publisher.Publish(domainEvent, cancellationToken);
        }
        reminder.ClearDomainEvents();
    }
}









