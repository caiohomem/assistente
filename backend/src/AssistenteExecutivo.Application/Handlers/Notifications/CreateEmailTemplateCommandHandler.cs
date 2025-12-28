using AssistenteExecutivo.Application.Commands.Notifications;
using AssistenteExecutivo.Application.Interfaces;
using AssistenteExecutivo.Domain.Notifications;
using MediatR;

namespace AssistenteExecutivo.Application.Handlers.Notifications;

public class CreateEmailTemplateCommandHandler : IRequestHandler<CreateEmailTemplateCommand, Guid>
{
    private readonly IEmailTemplateRepository _emailTemplateRepository;
    private readonly IUnitOfWork _unitOfWork;

    public CreateEmailTemplateCommandHandler(
        IEmailTemplateRepository emailTemplateRepository,
        IUnitOfWork unitOfWork)
    {
        _emailTemplateRepository = emailTemplateRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<Guid> Handle(CreateEmailTemplateCommand request, CancellationToken cancellationToken)
    {
        var template = new EmailTemplate(
            request.Name,
            request.TemplateType,
            request.Subject,
            request.HtmlBody);

        await _emailTemplateRepository.AddAsync(template, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return template.Id;
    }
}

