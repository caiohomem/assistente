using AssistenteExecutivo.Application.Commands.Workflow;
using AssistenteExecutivo.Application.Interfaces;
using AssistenteExecutivo.Domain.Interfaces;
using MediatR;

namespace AssistenteExecutivo.Application.Handlers.Workflow;

public class PauseWorkflowCommandHandler : IRequestHandler<PauseWorkflowCommand, bool>
{
    private readonly IWorkflowRepository _workflowRepository;
    private readonly IN8nProvider _n8nProvider;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IClock _clock;

    public PauseWorkflowCommandHandler(
        IWorkflowRepository workflowRepository,
        IN8nProvider n8nProvider,
        IUnitOfWork unitOfWork,
        IClock clock)
    {
        _workflowRepository = workflowRepository;
        _n8nProvider = n8nProvider;
        _unitOfWork = unitOfWork;
        _clock = clock;
    }

    public async Task<bool> Handle(PauseWorkflowCommand request, CancellationToken cancellationToken)
    {
        var workflow = await _workflowRepository.GetByIdAndOwnerAsync(request.WorkflowId, request.OwnerUserId, cancellationToken);
        if (workflow == null)
        {
            return false;
        }

        // Deactivate in n8n
        if (!string.IsNullOrEmpty(workflow.N8nWorkflowId))
        {
            await _n8nProvider.DeactivateWorkflowAsync(workflow.N8nWorkflowId, cancellationToken);
        }

        // Update domain
        workflow.Pause(_clock);

        // Persist
        await _workflowRepository.UpdateAsync(workflow, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return true;
    }
}
