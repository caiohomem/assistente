using AssistenteExecutivo.Application.DTOs;
using MediatR;

namespace AssistenteExecutivo.Application.Queries.Workflow;

public class GetExecutionStatusQuery : IRequest<WorkflowExecutionDto?>
{
    public Guid ExecutionId { get; set; }
    public Guid OwnerUserId { get; set; }
}
