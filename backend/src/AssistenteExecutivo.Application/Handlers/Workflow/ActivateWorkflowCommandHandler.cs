using AssistenteExecutivo.Application.Commands.Workflow;
using AssistenteExecutivo.Application.Interfaces;
using AssistenteExecutivo.Domain.Interfaces;
using MediatR;

namespace AssistenteExecutivo.Application.Handlers.Workflow;

public class ActivateWorkflowCommandHandler : IRequestHandler<ActivateWorkflowCommand, bool>
{
    private readonly IWorkflowRepository _workflowRepository;
    private readonly IN8nProvider _n8nProvider;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IClock _clock;
    private readonly IPublisher _publisher;

    public ActivateWorkflowCommandHandler(
        IWorkflowRepository workflowRepository,
        IN8nProvider n8nProvider,
        IUnitOfWork unitOfWork,
        IClock clock,
        IPublisher publisher)
    {
        _workflowRepository = workflowRepository;
        _n8nProvider = n8nProvider;
        _unitOfWork = unitOfWork;
        _clock = clock;
        _publisher = publisher;
    }

    public async Task<bool> Handle(ActivateWorkflowCommand request, CancellationToken cancellationToken)
    {
        var workflow = await _workflowRepository.GetByIdAndOwnerAsync(request.WorkflowId, request.OwnerUserId, cancellationToken);
        if (workflow == null)
        {
            return false;
        }

        if (string.IsNullOrEmpty(workflow.N8nWorkflowId))
        {
            return false;
        }

        // Activate in n8n
        await _n8nProvider.ActivateWorkflowAsync(workflow.N8nWorkflowId, cancellationToken);

        // Update domain
        workflow.Activate(_clock);

        // Persist
        await _workflowRepository.UpdateAsync(workflow, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        // Publish domain events
        foreach (var domainEvent in workflow.DomainEvents)
        {
            await _publisher.Publish(domainEvent, cancellationToken);
        }
        workflow.ClearDomainEvents();

        return true;
    }
}
