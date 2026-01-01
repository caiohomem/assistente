using MediatR;

namespace AssistenteExecutivo.Application.Commands.Workflow;

public class UpdateWorkflowRunCommand : IRequest<bool>
{
    public string RunId { get; set; } = string.Empty;
    public string? N8nExecutionId { get; set; }
    public string Status { get; set; } = string.Empty;
    public object? Result { get; set; }
    public object? Error { get; set; }
    public string? FinishedAt { get; set; }
}
