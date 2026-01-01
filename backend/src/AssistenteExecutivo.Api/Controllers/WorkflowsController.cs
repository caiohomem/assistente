using AssistenteExecutivo.Api.Auth;
using AssistenteExecutivo.Api.Extensions;
using AssistenteExecutivo.Application.Commands.Workflow;
using AssistenteExecutivo.Application.DTOs;
using AssistenteExecutivo.Application.Queries.Workflow;
using AssistenteExecutivo.Domain.Enums;
using MediatR;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AssistenteExecutivo.Api.Controllers;

[ApiController]
[Route("api/workflows")]
[Authorize(AuthenticationSchemes = $"{BffSessionAuthenticationDefaults.Scheme},{JwtBearerDefaults.AuthenticationScheme}")]
public sealed class WorkflowsController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly ILogger<WorkflowsController> _logger;

    public WorkflowsController(IMediator mediator, ILogger<WorkflowsController> logger)
    {
        _mediator = mediator;
        _logger = logger;
    }

    /// <summary>
    /// Creates a new workflow from a WorkflowSpec JSON.
    /// </summary>
    [HttpPost]
    [ProducesResponseType(typeof(CreateWorkflowResult), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(CreateWorkflowResult), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<CreateWorkflowResult>> CreateWorkflow(
        [FromBody] CreateWorkflowRequest request,
        CancellationToken cancellationToken)
    {
        var ownerUserId = await HttpContext.GetRequiredOwnerUserIdAsync(_mediator, cancellationToken);

        var command = new CreateWorkflowFromSpecCommand
        {
            OwnerUserId = ownerUserId,
            SpecJson = request.SpecJson,
            ActivateImmediately = request.ActivateImmediately
        };

        var result = await _mediator.Send(command, cancellationToken);

        if (!result.Success)
        {
            return BadRequest(result);
        }

        return CreatedAtAction(nameof(GetWorkflowById), new { id = result.WorkflowId }, result);
    }

    /// <summary>
    /// Lists all workflows for the current user.
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(List<WorkflowSummaryDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<List<WorkflowSummaryDto>>> ListWorkflows(
        [FromQuery] WorkflowStatus? status,
        CancellationToken cancellationToken)
    {
        var ownerUserId = await HttpContext.GetRequiredOwnerUserIdAsync(_mediator, cancellationToken);

        var query = new ListWorkflowsQuery
        {
            OwnerUserId = ownerUserId,
            FilterByStatus = status
        };

        var workflows = await _mediator.Send(query, cancellationToken);
        return Ok(workflows);
    }

    /// <summary>
    /// Gets a specific workflow by ID.
    /// </summary>
    [HttpGet("{id}")]
    [ProducesResponseType(typeof(WorkflowDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<WorkflowDto>> GetWorkflowById(
        Guid id,
        CancellationToken cancellationToken)
    {
        var ownerUserId = await HttpContext.GetRequiredOwnerUserIdAsync(_mediator, cancellationToken);

        var query = new GetWorkflowByIdQuery
        {
            WorkflowId = id,
            OwnerUserId = ownerUserId
        };

        var workflow = await _mediator.Send(query, cancellationToken);

        if (workflow == null)
        {
            return NotFound();
        }

        return Ok(workflow);
    }

    /// <summary>
    /// Executes a workflow.
    /// </summary>
    [HttpPost("{id}/execute")]
    [ProducesResponseType(typeof(ExecuteWorkflowResult), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ExecuteWorkflowResult), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ExecuteWorkflowResult>> ExecuteWorkflow(
        Guid id,
        [FromBody] ExecuteWorkflowRequest? request,
        CancellationToken cancellationToken)
    {
        var ownerUserId = await HttpContext.GetRequiredOwnerUserIdAsync(_mediator, cancellationToken);

        var command = new ExecuteWorkflowCommand
        {
            WorkflowId = id,
            OwnerUserId = ownerUserId,
            InputJson = request?.InputJson
        };

        var result = await _mediator.Send(command, cancellationToken);

        if (!result.Success)
        {
            return BadRequest(result);
        }

        return Ok(result);
    }

    /// <summary>
    /// Lists execution history for a workflow or all workflows.
    /// </summary>
    [HttpGet("executions")]
    [ProducesResponseType(typeof(List<WorkflowExecutionSummaryDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<List<WorkflowExecutionSummaryDto>>> ListExecutions(
        [FromQuery] Guid? workflowId,
        [FromQuery] int limit = 50,
        CancellationToken cancellationToken = default)
    {
        var ownerUserId = await HttpContext.GetRequiredOwnerUserIdAsync(_mediator, cancellationToken);

        var query = new ListWorkflowExecutionsQuery
        {
            OwnerUserId = ownerUserId,
            WorkflowId = workflowId,
            Limit = limit
        };

        var executions = await _mediator.Send(query, cancellationToken);
        return Ok(executions);
    }

    /// <summary>
    /// Gets execution status by ID.
    /// </summary>
    [HttpGet("executions/{id}")]
    [ProducesResponseType(typeof(WorkflowExecutionDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<WorkflowExecutionDto>> GetExecutionStatus(
        Guid id,
        CancellationToken cancellationToken)
    {
        var ownerUserId = await HttpContext.GetRequiredOwnerUserIdAsync(_mediator, cancellationToken);

        var query = new GetExecutionStatusQuery
        {
            ExecutionId = id,
            OwnerUserId = ownerUserId
        };

        var execution = await _mediator.Send(query, cancellationToken);

        if (execution == null)
        {
            return NotFound();
        }

        return Ok(execution);
    }

    /// <summary>
    /// Lists pending approval requests.
    /// </summary>
    [HttpGet("pending-approvals")]
    [ProducesResponseType(typeof(List<WorkflowExecutionDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<List<WorkflowExecutionDto>>> GetPendingApprovals(
        CancellationToken cancellationToken)
    {
        var ownerUserId = await HttpContext.GetRequiredOwnerUserIdAsync(_mediator, cancellationToken);

        var query = new GetPendingApprovalsQuery
        {
            OwnerUserId = ownerUserId
        };

        var executions = await _mediator.Send(query, cancellationToken);
        return Ok(executions);
    }

    /// <summary>
    /// Approves a pending workflow step.
    /// </summary>
    [HttpPost("executions/{id}/approve")]
    [ProducesResponseType(typeof(ApproveStepResult), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApproveStepResult), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<ApproveStepResult>> ApproveStep(
        Guid id,
        CancellationToken cancellationToken)
    {
        var ownerUserId = await HttpContext.GetRequiredOwnerUserIdAsync(_mediator, cancellationToken);

        var command = new ApproveWorkflowStepCommand
        {
            ExecutionId = id,
            ApprovedByUserId = ownerUserId
        };

        var result = await _mediator.Send(command, cancellationToken);

        if (!result.Success)
        {
            return BadRequest(result);
        }

        return Ok(result);
    }

    /// <summary>
    /// Activates a workflow.
    /// </summary>
    [HttpPost("{id}/activate")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> ActivateWorkflow(
        Guid id,
        CancellationToken cancellationToken)
    {
        var ownerUserId = await HttpContext.GetRequiredOwnerUserIdAsync(_mediator, cancellationToken);

        var command = new ActivateWorkflowCommand
        {
            WorkflowId = id,
            OwnerUserId = ownerUserId
        };

        var result = await _mediator.Send(command, cancellationToken);

        if (!result)
        {
            return NotFound();
        }

        return NoContent();
    }

    /// <summary>
    /// Pauses a workflow.
    /// </summary>
    [HttpPost("{id}/pause")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> PauseWorkflow(
        Guid id,
        CancellationToken cancellationToken)
    {
        var ownerUserId = await HttpContext.GetRequiredOwnerUserIdAsync(_mediator, cancellationToken);

        var command = new PauseWorkflowCommand
        {
            WorkflowId = id,
            OwnerUserId = ownerUserId
        };

        var result = await _mediator.Send(command, cancellationToken);

        if (!result)
        {
            return NotFound();
        }

        return NoContent();
    }

    /// <summary>
    /// Archives (soft deletes) a workflow.
    /// </summary>
    [HttpDelete("{id}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> ArchiveWorkflow(
        Guid id,
        CancellationToken cancellationToken)
    {
        var ownerUserId = await HttpContext.GetRequiredOwnerUserIdAsync(_mediator, cancellationToken);

        var command = new ArchiveWorkflowCommand
        {
            WorkflowId = id,
            OwnerUserId = ownerUserId
        };

        var result = await _mediator.Send(command, cancellationToken);

        if (!result)
        {
            return NotFound();
        }

        return NoContent();
    }

    #region System Endpoints (called by n8n workflows)

    /// <summary>
    /// Saves a workflow spec (called by Flow Builder).
    /// </summary>
    [HttpPost("specs")]
    [AllowAnonymous] // Uses internal API key authentication
    [ProducesResponseType(typeof(SaveSpecResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<SaveSpecResponse>> SaveSpec(
        [FromBody] SaveSpecRequest request,
        [FromHeader(Name = "X-Internal-Key")] string? internalKey,
        CancellationToken cancellationToken)
    {
        // Validate internal API key
        if (!ValidateInternalKey(internalKey))
        {
            return Unauthorized();
        }

        var command = new SaveWorkflowSpecCommand
        {
            Name = request.Name,
            Description = request.Description,
            SpecJson = request.SpecJson,
            TenantId = request.TenantId,
            RequestedBy = request.RequestedBy,
            IdempotencyKey = request.IdempotencyKey
        };

        var result = await _mediator.Send(command, cancellationToken);

        if (!result.Success)
        {
            return BadRequest(new { error = result.ErrorMessage });
        }

        return Ok(new SaveSpecResponse
        {
            SpecId = result.SpecId!.Value,
            SpecVersion = result.SpecVersion
        });
    }

    /// <summary>
    /// Binds a spec to an n8n workflow (called by Flow Builder).
    /// </summary>
    [HttpPut("specs/{specId}/bind")]
    [AllowAnonymous]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> BindSpecToWorkflow(
        Guid specId,
        [FromBody] BindSpecRequest request,
        [FromHeader(Name = "X-Internal-Key")] string? internalKey,
        CancellationToken cancellationToken)
    {
        if (!ValidateInternalKey(internalKey))
        {
            return Unauthorized();
        }

        var command = new BindSpecToWorkflowCommand
        {
            SpecId = specId,
            N8nWorkflowId = request.N8nWorkflowId,
            CompiledAt = request.CompiledAt,
            Checksum = request.Checksum
        };

        var result = await _mediator.Send(command, cancellationToken);

        if (!result)
        {
            return NotFound();
        }

        return NoContent();
    }

    /// <summary>
    /// Resolves a spec ID to its n8n workflow ID (called by Flow Runner).
    /// </summary>
    [HttpGet("specs/{specId}/resolve")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(ResolveSpecResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ResolveSpecResponse>> ResolveSpec(
        Guid specId,
        [FromQuery] int? version,
        [FromHeader(Name = "X-Internal-Key")] string? internalKey,
        CancellationToken cancellationToken)
    {
        if (!ValidateInternalKey(internalKey))
        {
            return Unauthorized();
        }

        var query = new ResolveSpecToWorkflowQuery
        {
            SpecId = specId,
            Version = version
        };

        var result = await _mediator.Send(query, cancellationToken);

        if (result == null)
        {
            return NotFound();
        }

        return Ok(new ResolveSpecResponse
        {
            N8nWorkflowId = result.N8nWorkflowId,
            SpecVersion = result.SpecVersion
        });
    }

    /// <summary>
    /// Checks if a run with the given idempotency key already exists (called by Flow Runner).
    /// </summary>
    [HttpGet("runs/check")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(CheckIdempotencyResponse), StatusCodes.Status200OK)]
    public async Task<ActionResult<CheckIdempotencyResponse>> CheckIdempotency(
        [FromQuery] string idempotencyKey,
        [FromHeader(Name = "X-Internal-Key")] string? internalKey,
        CancellationToken cancellationToken)
    {
        if (!ValidateInternalKey(internalKey))
        {
            return Unauthorized();
        }

        var query = new CheckRunIdempotencyQuery
        {
            IdempotencyKey = idempotencyKey
        };

        var result = await _mediator.Send(query, cancellationToken);

        return Ok(new CheckIdempotencyResponse
        {
            Exists = result.Exists,
            RunId = result.RunId,
            ExecutionId = result.ExecutionId,
            Status = result.Status,
            Result = result.Result
        });
    }

    /// <summary>
    /// Registers a new workflow run (called by Flow Runner).
    /// </summary>
    [HttpPost("runs")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(RegisterRunResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<RegisterRunResponse>> RegisterRun(
        [FromBody] RegisterRunRequest request,
        [FromHeader(Name = "X-Internal-Key")] string? internalKey,
        CancellationToken cancellationToken)
    {
        if (!ValidateInternalKey(internalKey))
        {
            return Unauthorized();
        }

        var command = new RegisterWorkflowRunCommand
        {
            RunId = request.RunId,
            WorkflowId = request.WorkflowId,
            TenantId = request.TenantId,
            RequestedBy = request.RequestedBy,
            IdempotencyKey = request.IdempotencyKey,
            Inputs = request.Inputs,
            StartedAt = request.StartedAt,
            Status = request.Status
        };

        var result = await _mediator.Send(command, cancellationToken);

        if (!result.Success)
        {
            return BadRequest(new { error = result.ErrorMessage });
        }

        return Ok(new RegisterRunResponse
        {
            RunId = result.RunId!
        });
    }

    /// <summary>
    /// Updates a workflow run status (called by Flow Runner).
    /// </summary>
    [HttpPut("runs/{runId}")]
    [AllowAnonymous]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateRun(
        string runId,
        [FromBody] UpdateRunRequest request,
        [FromHeader(Name = "X-Internal-Key")] string? internalKey,
        CancellationToken cancellationToken)
    {
        if (!ValidateInternalKey(internalKey))
        {
            return Unauthorized();
        }

        var command = new UpdateWorkflowRunCommand
        {
            RunId = runId,
            N8nExecutionId = request.N8nExecutionId,
            Status = request.Status,
            Result = request.Result,
            Error = request.Error,
            FinishedAt = request.FinishedAt
        };

        var result = await _mediator.Send(command, cancellationToken);

        if (!result)
        {
            return NotFound();
        }

        return NoContent();
    }

    private bool ValidateInternalKey(string? key)
    {
        // In production, validate against a configured internal API key
        // For now, accept any non-empty key or allow if coming from localhost
        if (string.IsNullOrEmpty(key))
        {
            var remoteIp = HttpContext.Connection.RemoteIpAddress?.ToString();
            return remoteIp == "127.0.0.1" || remoteIp == "::1" || remoteIp?.StartsWith("10.") == true;
        }
        return true; // TODO: Validate against configured key
    }

    #endregion
}

#region Request/Response DTOs

public class CreateWorkflowRequest
{
    public string SpecJson { get; set; } = string.Empty;
    public bool ActivateImmediately { get; set; }
}

public class ExecuteWorkflowRequest
{
    public string? InputJson { get; set; }
}

// System endpoint DTOs
public class SaveSpecRequest
{
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string SpecJson { get; set; } = string.Empty;
    public string TenantId { get; set; } = string.Empty;
    public string RequestedBy { get; set; } = string.Empty;
    public string? IdempotencyKey { get; set; }
}

public class SaveSpecResponse
{
    public Guid SpecId { get; set; }
    public int SpecVersion { get; set; }
}

public class BindSpecRequest
{
    public string N8nWorkflowId { get; set; } = string.Empty;
    public string? CompiledAt { get; set; }
    public string? Checksum { get; set; }
}

public class ResolveSpecResponse
{
    public string N8nWorkflowId { get; set; } = string.Empty;
    public int SpecVersion { get; set; }
}

public class CheckIdempotencyResponse
{
    public bool Exists { get; set; }
    public string? RunId { get; set; }
    public string? ExecutionId { get; set; }
    public string? Status { get; set; }
    public object? Result { get; set; }
}

public class RegisterRunRequest
{
    public string RunId { get; set; } = string.Empty;
    public string WorkflowId { get; set; } = string.Empty;
    public string TenantId { get; set; } = string.Empty;
    public string RequestedBy { get; set; } = string.Empty;
    public string? IdempotencyKey { get; set; }
    public object? Inputs { get; set; }
    public string? StartedAt { get; set; }
    public string Status { get; set; } = "Running";
}

public class RegisterRunResponse
{
    public string RunId { get; set; } = string.Empty;
}

public class UpdateRunRequest
{
    public string? N8nExecutionId { get; set; }
    public string Status { get; set; } = string.Empty;
    public object? Result { get; set; }
    public object? Error { get; set; }
    public string? FinishedAt { get; set; }
}

#endregion
