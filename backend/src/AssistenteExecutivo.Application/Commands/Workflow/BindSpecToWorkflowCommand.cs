using MediatR;

namespace AssistenteExecutivo.Application.Commands.Workflow;

public class BindSpecToWorkflowCommand : IRequest<bool>
{
    public Guid SpecId { get; set; }
    public string N8nWorkflowId { get; set; } = string.Empty;
    public string? CompiledAt { get; set; }
    public string? Checksum { get; set; }
}
