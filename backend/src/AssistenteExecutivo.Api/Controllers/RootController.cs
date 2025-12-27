using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;

namespace AssistenteExecutivo.Api.Controllers;

/// <summary>
/// Root controller to handle root path requests and provide API information.
/// </summary>
[ApiController]
[Route("")]
public sealed class RootController : ControllerBase
{
    private readonly IWebHostEnvironment _environment;

    public RootController(IWebHostEnvironment environment)
    {
        _environment = environment;
    }

    /// <summary>
    /// Root endpoint - returns API information.
    /// </summary>
    [HttpGet]
    [AllowAnonymous]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public IActionResult GetRoot()
    {
        return Ok(new
        {
            name = "Assistente Executivo API",
            version = "v1",
            environment = _environment.EnvironmentName,
            status = "running",
            timestamp = DateTime.UtcNow,
            endpoints = new
            {
                health = "/health",
                swagger = _environment.IsDevelopment() ? "/swagger" : null,
                api = "/api"
            }
        });
    }

    /// <summary>
    /// Health check endpoint for monitoring and load balancers.
    /// </summary>
    [HttpGet("health")]
    [AllowAnonymous]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public IActionResult GetHealth()
    {
        return Ok(new
        {
            status = "healthy",
            timestamp = DateTime.UtcNow,
            environment = _environment.EnvironmentName
        });
    }
}

