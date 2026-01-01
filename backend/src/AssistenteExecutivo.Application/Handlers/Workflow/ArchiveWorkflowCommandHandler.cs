using AssistenteExecutivo.Application.Commands.Workflow;
using AssistenteExecutivo.Application.Interfaces;
using AssistenteExecutivo.Domain.Interfaces;
using MediatR;

namespace AssistenteExecutivo.Application.Handlers.Workflow;

public class ArchiveWorkflowCommandHandler : IRequestHandler<ArchiveWorkflowCommand, bool>
{
    private readonly IWorkflowRepository _workflowRepository;
    private readonly IN8nProvider _n8nProvider;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IClock _clock;

    public ArchiveWorkflowCommandHandler(
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

    public async Task<bool> Handle(ArchiveWorkflowCommand request, CancellationToken cancellationToken)
    {
        var workflow = await _workflowRepository.GetByIdAndOwnerAsync(request.WorkflowId, request.OwnerUserId, cancellationToken);
        if (workflow == null)
        {
            return false;
        }

        // Delete from n8n
        if (!string.IsNullOrEmpty(workflow.N8nWorkflowId))
        {
            try
            {
                await _n8nProvider.DeleteWorkflowAsync(workflow.N8nWorkflowId, cancellationToken);
            }
            catch
            {
                // Log but don't fail - workflow may already be deleted
            }
        }

        // Archive in domain
        workflow.Archive(_clock);

        // Persist
        await _workflowRepository.UpdateAsync(workflow, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return true;
    }
}
