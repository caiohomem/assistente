using AssistenteExecutivo.Application.Commands.Notifications;
using AssistenteExecutivo.Application.Interfaces;
using AssistenteExecutivo.Domain.Exceptions;
using MediatR;

namespace AssistenteExecutivo.Application.Handlers.Notifications;

public class UpdateEmailTemplateCommandHandler : IRequestHandler<UpdateEmailTemplateCommand>
{
    private readonly IEmailTemplateRepository _emailTemplateRepository;
    private readonly IUnitOfWork _unitOfWork;

    public UpdateEmailTemplateCommandHandler(
        IEmailTemplateRepository emailTemplateRepository,
        IUnitOfWork unitOfWork)
    {
        _emailTemplateRepository = emailTemplateRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task Handle(UpdateEmailTemplateCommand request, CancellationToken cancellationToken)
    {
        var template = await _emailTemplateRepository.GetByIdAsync(request.EmailTemplateId, cancellationToken);
        if (template == null)
            throw new DomainException("Template de email não encontrado");

        if (!string.IsNullOrWhiteSpace(request.Name))
        {
            template.UpdateName(request.Name);
        }

        if (!string.IsNullOrWhiteSpace(request.Subject) || !string.IsNullOrWhiteSpace(request.HtmlBody))
        {
            var subject = request.Subject ?? template.Subject;
            var htmlBody = request.HtmlBody ?? template.HtmlBody;
            template.UpdateContent(subject, htmlBody);
        }

        await _emailTemplateRepository.UpdateAsync(template, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }
}

