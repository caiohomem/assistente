using AssistenteExecutivo.Application.Commands.Automation;
using AssistenteExecutivo.Application.Interfaces;
using AssistenteExecutivo.Domain.Exceptions;
using MediatR;

namespace AssistenteExecutivo.Application.Handlers.Automation;

public class DeleteReminderCommandHandler : IRequestHandler<DeleteReminderCommand>
{
    private readonly IReminderRepository _reminderRepository;
    private readonly IUnitOfWork _unitOfWork;

    public DeleteReminderCommandHandler(
        IReminderRepository reminderRepository,
        IUnitOfWork unitOfWork)
    {
        _reminderRepository = reminderRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task Handle(DeleteReminderCommand request, CancellationToken cancellationToken)
    {
        if (request.ReminderId == Guid.Empty)
            throw new DomainException("Domain:ReminderIdObrigatorio");

        if (request.OwnerUserId == Guid.Empty)
            throw new DomainException("Domain:OwnerUserIdObrigatorio");

        var reminder = await _reminderRepository.GetByIdAsync(request.ReminderId, request.OwnerUserId, cancellationToken);
        if (reminder == null)
            throw new DomainException("Domain:ReminderNaoEncontrado");

        await _reminderRepository.DeleteAsync(reminder, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }
}









