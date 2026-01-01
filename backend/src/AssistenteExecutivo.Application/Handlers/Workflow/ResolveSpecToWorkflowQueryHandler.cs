using AssistenteExecutivo.Application.Interfaces;
using AssistenteExecutivo.Application.Queries.Workflow;
using MediatR;
using Microsoft.Extensions.Logging;

namespace AssistenteExecutivo.Application.Handlers.Workflow;

public class ResolveSpecToWorkflowQueryHandler : IRequestHandler<ResolveSpecToWorkflowQuery, ResolveSpecResult?>
{
    private readonly IWorkflowRepository _workflowRepository;
    private readonly ILogger<ResolveSpecToWorkflowQueryHandler> _logger;

    public ResolveSpecToWorkflowQueryHandler(
        IWorkflowRepository workflowRepository,
        ILogger<ResolveSpecToWorkflowQueryHandler> logger)
    {
        _workflowRepository = workflowRepository;
        _logger = logger;
    }

    public async Task<ResolveSpecResult?> Handle(ResolveSpecToWorkflowQuery request, CancellationToken cancellationToken)
    {
        var workflow = await _workflowRepository.GetByIdAsync(request.SpecId, cancellationToken);

        if (workflow == null || string.IsNullOrEmpty(workflow.N8nWorkflowId))
        {
            _logger.LogWarning("Could not resolve spec {SpecId} to workflow", request.SpecId);
            return null;
        }

        // If specific version requested, check it matches
        if (request.Version.HasValue && workflow.SpecVersion != request.Version.Value)
        {
            _logger.LogWarning("Spec version mismatch: requested {Requested}, actual {Actual}",
                request.Version, workflow.SpecVersion);
            // Could implement version history lookup here
        }

        return new ResolveSpecResult
        {
            N8nWorkflowId = workflow.N8nWorkflowId,
            SpecVersion = workflow.SpecVersion
        };
    }
}
