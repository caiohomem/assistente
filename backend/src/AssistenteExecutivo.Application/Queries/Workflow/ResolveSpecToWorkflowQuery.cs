using MediatR;

namespace AssistenteExecutivo.Application.Queries.Workflow;

public class ResolveSpecToWorkflowQuery : IRequest<ResolveSpecResult?>
{
    public Guid SpecId { get; set; }
    public int? Version { get; set; }
}

public class ResolveSpecResult
{
    public string N8nWorkflowId { get; set; } = string.Empty;
    public int SpecVersion { get; set; }
}
