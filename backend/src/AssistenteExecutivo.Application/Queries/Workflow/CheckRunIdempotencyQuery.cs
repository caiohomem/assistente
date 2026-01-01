using MediatR;

namespace AssistenteExecutivo.Application.Queries.Workflow;

public class CheckRunIdempotencyQuery : IRequest<CheckRunIdempotencyResult>
{
    public string IdempotencyKey { get; set; } = string.Empty;
}

public class CheckRunIdempotencyResult
{
    public bool Exists { get; set; }
    public string? RunId { get; set; }
    public string? ExecutionId { get; set; }
    public string? Status { get; set; }
    public object? Result { get; set; }
}
