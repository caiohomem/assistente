using AssistenteExecutivo.Application.Commands.Notifications;
using AssistenteExecutivo.Application.Interfaces;
using AssistenteExecutivo.Domain.Exceptions;
using MediatR;

namespace AssistenteExecutivo.Application.Handlers.Notifications;

public class DeleteEmailTemplateCommandHandler : IRequestHandler<DeleteEmailTemplateCommand>
{
    private readonly IEmailTemplateRepository _emailTemplateRepository;
    private readonly IUnitOfWork _unitOfWork;

    public DeleteEmailTemplateCommandHandler(
        IEmailTemplateRepository emailTemplateRepository,
        IUnitOfWork unitOfWork)
    {
        _emailTemplateRepository = emailTemplateRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task Handle(DeleteEmailTemplateCommand request, CancellationToken cancellationToken)
    {
        if (request.EmailTemplateId == Guid.Empty)
            throw new DomainException("ID do template de email é obrigatório");

        var template = await _emailTemplateRepository.GetByIdAsync(request.EmailTemplateId, cancellationToken);
        if (template == null)
            throw new DomainException("Template de email não encontrado");

        await _emailTemplateRepository.DeleteAsync(template, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }
}

