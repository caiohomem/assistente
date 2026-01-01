using AssistenteExecutivo.Application.Interfaces;
using AssistenteExecutivo.Application.Queries.Workflow;
using MediatR;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace AssistenteExecutivo.Application.Handlers.Workflow;

public class CheckRunIdempotencyQueryHandler : IRequestHandler<CheckRunIdempotencyQuery, CheckRunIdempotencyResult>
{
    private readonly IWorkflowExecutionRepository _executionRepository;
    private readonly ILogger<CheckRunIdempotencyQueryHandler> _logger;

    public CheckRunIdempotencyQueryHandler(
        IWorkflowExecutionRepository executionRepository,
        ILogger<CheckRunIdempotencyQueryHandler> logger)
    {
        _executionRepository = executionRepository;
        _logger = logger;
    }

    public async Task<CheckRunIdempotencyResult> Handle(CheckRunIdempotencyQuery request, CancellationToken cancellationToken)
    {
        var execution = await _executionRepository.GetByIdempotencyKeyAsync(request.IdempotencyKey, cancellationToken);

        if (execution == null)
        {
            return new CheckRunIdempotencyResult { Exists = false };
        }

        _logger.LogInformation("Found existing execution for idempotency key: {Key}", request.IdempotencyKey);

        object? result = null;
        if (!string.IsNullOrEmpty(execution.OutputJson))
        {
            try
            {
                result = JsonSerializer.Deserialize<object>(execution.OutputJson);
            }
            catch
            {
                result = execution.OutputJson;
            }
        }

        return new CheckRunIdempotencyResult
        {
            Exists = true,
            RunId = execution.ExecutionId.ToString(),
            ExecutionId = execution.N8nExecutionId,
            Status = execution.Status.ToString(),
            Result = result
        };
    }
}
