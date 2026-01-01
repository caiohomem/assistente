using AssistenteExecutivo.Application.DTOs;
using AssistenteExecutivo.Application.Queries.Plans;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AssistenteExecutivo.Api.Controllers;

[ApiController]
[Route("api/plans")]
[AllowAnonymous] // Plans são públicos, não requerem autenticação
public sealed class PlansController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly ILogger<PlansController> _logger;

    public PlansController(
        IMediator mediator,
        ILogger<PlansController> logger)
    {
        _mediator = mediator;
        _logger = logger;
    }

    /// <summary>
    /// Lista todos os planos disponíveis (apenas ativos por padrão).
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(List<PlanDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<List<PlanDto>>> ListPlans(
        [FromQuery] bool includeInactive = false,
        CancellationToken cancellationToken = default)
    {
        var query = new ListPlansQuery
        {
            IncludeInactive = includeInactive
        };

        var plans = await _mediator.Send(query, cancellationToken);
        return Ok(plans);
    }
}












