using AssistenteExecutivo.Application.Commands.Automation;
using AssistenteExecutivo.Application.Interfaces;
using AssistenteExecutivo.Domain.Exceptions;
using MediatR;

namespace AssistenteExecutivo.Application.Handlers.Automation;

public class DeleteTemplateCommandHandler : IRequestHandler<DeleteTemplateCommand>
{
    private readonly ITemplateRepository _templateRepository;
    private readonly IUnitOfWork _unitOfWork;

    public DeleteTemplateCommandHandler(
        ITemplateRepository templateRepository,
        IUnitOfWork unitOfWork)
    {
        _templateRepository = templateRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task Handle(DeleteTemplateCommand request, CancellationToken cancellationToken)
    {
        if (request.TemplateId == Guid.Empty)
            throw new DomainException("Domain:TemplateIdObrigatorio");

        if (request.OwnerUserId == Guid.Empty)
            throw new DomainException("Domain:OwnerUserIdObrigatorio");

        var template = await _templateRepository.GetByIdAsync(request.TemplateId, request.OwnerUserId, cancellationToken);
        if (template == null)
            throw new DomainException("Domain:TemplateNaoEncontrado");

        await _templateRepository.DeleteAsync(template, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }
}









