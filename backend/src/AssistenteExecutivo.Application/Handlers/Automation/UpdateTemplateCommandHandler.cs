using AssistenteExecutivo.Application.Commands.Automation;
using AssistenteExecutivo.Application.Interfaces;
using AssistenteExecutivo.Domain.Exceptions;
using AssistenteExecutivo.Domain.Interfaces;
using MediatR;

namespace AssistenteExecutivo.Application.Handlers.Automation;

public class UpdateTemplateCommandHandler : IRequestHandler<UpdateTemplateCommand>
{
    private readonly ITemplateRepository _templateRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IClock _clock;

    public UpdateTemplateCommandHandler(
        ITemplateRepository templateRepository,
        IUnitOfWork unitOfWork,
        IClock clock)
    {
        _templateRepository = templateRepository;
        _unitOfWork = unitOfWork;
        _clock = clock;
    }

    public async Task Handle(UpdateTemplateCommand request, CancellationToken cancellationToken)
    {
        var template = await _templateRepository.GetByIdAsync(request.TemplateId, request.OwnerUserId, cancellationToken);
        if (template == null)
            throw new DomainException("Domain:TemplateNaoEncontrado");

        if (!string.IsNullOrWhiteSpace(request.Name))
        {
            template.UpdateName(request.Name, _clock);
        }

        if (!string.IsNullOrWhiteSpace(request.Body))
        {
            template.UpdateBody(request.Body, _clock);
        }

        if (request.PlaceholdersSchema != null)
        {
            template.UpdatePlaceholdersSchema(request.PlaceholdersSchema, _clock);
        }

        if (request.Active.HasValue)
        {
            if (request.Active.Value)
                template.Activate(_clock);
            else
                template.Deactivate(_clock);
        }

        await _templateRepository.UpdateAsync(template, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }
}







