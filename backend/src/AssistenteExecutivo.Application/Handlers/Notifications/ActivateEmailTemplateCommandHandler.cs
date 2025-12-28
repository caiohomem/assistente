using AssistenteExecutivo.Application.Commands.Notifications;
using AssistenteExecutivo.Application.Interfaces;
using AssistenteExecutivo.Domain.Exceptions;
using MediatR;

namespace AssistenteExecutivo.Application.Handlers.Notifications;

public class ActivateEmailTemplateCommandHandler : IRequestHandler<ActivateEmailTemplateCommand>
{
    private readonly IEmailTemplateRepository _emailTemplateRepository;
    private readonly IUnitOfWork _unitOfWork;

    public ActivateEmailTemplateCommandHandler(
        IEmailTemplateRepository emailTemplateRepository,
        IUnitOfWork unitOfWork)
    {
        _emailTemplateRepository = emailTemplateRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task Handle(ActivateEmailTemplateCommand request, CancellationToken cancellationToken)
    {
        var template = await _emailTemplateRepository.GetByIdAsync(request.EmailTemplateId, cancellationToken);
        if (template == null)
            throw new DomainException("Template de email não encontrado");

        template.Activate();
        await _emailTemplateRepository.UpdateAsync(template, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }
}

