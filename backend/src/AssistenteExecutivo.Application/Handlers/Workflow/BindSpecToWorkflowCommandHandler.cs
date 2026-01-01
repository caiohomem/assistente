using AssistenteExecutivo.Application.Commands.Workflow;
using AssistenteExecutivo.Application.Interfaces;
using AssistenteExecutivo.Domain.Interfaces;
using MediatR;
using Microsoft.Extensions.Logging;

namespace AssistenteExecutivo.Application.Handlers.Workflow;

public class BindSpecToWorkflowCommandHandler : IRequestHandler<BindSpecToWorkflowCommand, bool>
{
    private readonly IWorkflowRepository _workflowRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IClock _clock;
    private readonly ILogger<BindSpecToWorkflowCommandHandler> _logger;

    public BindSpecToWorkflowCommandHandler(
        IWorkflowRepository workflowRepository,
        IUnitOfWork unitOfWork,
        IClock clock,
        ILogger<BindSpecToWorkflowCommandHandler> logger)
    {
        _workflowRepository = workflowRepository;
        _unitOfWork = unitOfWork;
        _clock = clock;
        _logger = logger;
    }

    public async Task<bool> Handle(BindSpecToWorkflowCommand request, CancellationToken cancellationToken)
    {
        var workflow = await _workflowRepository.GetByIdAsync(request.SpecId, cancellationToken);
        if (workflow == null)
        {
            _logger.LogWarning("Workflow not found for spec binding: {SpecId}", request.SpecId);
            return false;
        }

        workflow.BindToN8n(request.N8nWorkflowId, _clock);

        await _workflowRepository.UpdateAsync(workflow, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Bound spec {SpecId} to n8n workflow {N8nWorkflowId}",
            request.SpecId, request.N8nWorkflowId);

        return true;
    }
}
