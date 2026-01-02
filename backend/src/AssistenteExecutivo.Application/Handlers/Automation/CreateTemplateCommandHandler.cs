using AssistenteExecutivo.Application.Commands.Automation;
using AssistenteExecutivo.Application.Interfaces;
using AssistenteExecutivo.Domain.Entities;
using AssistenteExecutivo.Domain.Exceptions;
using AssistenteExecutivo.Domain.Interfaces;
using MediatR;

namespace AssistenteExecutivo.Application.Handlers.Automation;

public class CreateTemplateCommandHandler : IRequestHandler<CreateTemplateCommand, Guid>
{
    private readonly ITemplateRepository _templateRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IClock _clock;
    private readonly IPublisher _publisher;

    public CreateTemplateCommandHandler(
        ITemplateRepository templateRepository,
        IUnitOfWork unitOfWork,
        IClock clock,
        IPublisher publisher)
    {
        _templateRepository = templateRepository;
        _unitOfWork = unitOfWork;
        _clock = clock;
        _publisher = publisher;
    }

    public async Task<Guid> Handle(CreateTemplateCommand request, CancellationToken cancellationToken)
    {
        if (request.OwnerUserId == Guid.Empty)
            throw new DomainException("Domain:OwnerUserIdObrigatorio");

        var templateId = Guid.NewGuid();
        var template = Template.Create(
            templateId,
            request.OwnerUserId,
            request.Name,
            request.Type,
            request.Body,
            _clock);

        if (!string.IsNullOrWhiteSpace(request.PlaceholdersSchema))
        {
            template.UpdatePlaceholdersSchema(request.PlaceholdersSchema, _clock);
        }

        await _templateRepository.AddAsync(template, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        // Publish domain events
        foreach (var domainEvent in template.DomainEvents)
        {
            await _publisher.Publish(domainEvent, cancellationToken);
        }
        template.ClearDomainEvents();

        return templateId;
    }
}









