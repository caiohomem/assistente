using System.IdentityModel.Tokens.Jwt;
using AssistenteExecutivo.Api.Auth;
using AssistenteExecutivo.Api.Extensions;
using AssistenteExecutivo.Application.Commands.Auth;
using MediatR;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AssistenteExecutivo.Api.Controllers;

[ApiController]
[Route("api")]
public sealed class MeController : ControllerBase
{
    private readonly IConfiguration _config;
    private readonly IMediator _mediator;

    public MeController(IConfiguration config, IMediator mediator)
    {
        _config = config;
        _mediator = mediator;
    }

    /// <summary>
    /// Minimal protected endpoint for mobile clients.
    /// Requires a valid Keycloak access_token as Bearer token.
    /// </summary>
    [HttpGet("me")]
    [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
    public IActionResult Me()
    {
        var claims = User.Claims
            .GroupBy(c => c.Type)
            .ToDictionary(g => g.Key, g => g.Select(x => x.Value).ToArray());

        return Ok(new
        {
            authenticated = User.Identity?.IsAuthenticated ?? false,
            name = User.Identity?.Name,
            claims
        });
    }

    /// <summary>
    /// Debug endpoint - shows JWT config and token info without requiring auth
    /// </summary>
    [HttpGet("debug-jwt")]
    [AllowAnonymous]
    public IActionResult DebugJwt()
    {
        var authHeader = Request.Headers.Authorization.FirstOrDefault();
        var token = authHeader?.Replace("Bearer ", "", StringComparison.OrdinalIgnoreCase);

        var baseUrl = _config["Keycloak:BaseUrl"];
        var realm = _config["Keycloak:Realm"] ?? "assistenteexecutivo";
        var expectedIssuer = !string.IsNullOrWhiteSpace(baseUrl)
            ? $"{baseUrl.TrimEnd('/')}/realms/{realm}"
            : throw new InvalidOperationException("Keycloak:BaseUrl deve estar configurado em appsettings");

        string? tokenIssuer = null;
        string? tokenAudience = null;
        DateTime? tokenExpires = null;
        string? tokenError = null;

        if (!string.IsNullOrEmpty(token))
        {
            var handler = new JwtSecurityTokenHandler();
            var jwt = handler.ReadJwtToken(token);
            tokenIssuer = jwt.Issuer;
            tokenAudience = string.Join(", ", jwt.Audiences);
            tokenExpires = jwt.ValidTo;
        }

        return Ok(new
        {
            config = new
            {
                keycloak_BaseUrl = baseUrl,
                keycloak_Realm = realm,
                expectedIssuer
            },
            token = new
            {
                received = !string.IsNullOrEmpty(token),
                issuer = tokenIssuer,
                audience = tokenAudience,
                expires = tokenExpires,
                error = tokenError,
                issuerMatch = tokenIssuer == expectedIssuer
            }
        });
    }

    /// <summary>
    /// Exclui o perfil do usuário autenticado (soft delete).
    /// </summary>
    [HttpDelete("me")]
    [Authorize(AuthenticationSchemes = $"{BffSessionAuthenticationDefaults.Scheme},{JwtBearerDefaults.AuthenticationScheme}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteProfile(CancellationToken cancellationToken = default)
    {
        var userId = await HttpContext.GetRequiredOwnerUserIdAsync(_mediator, cancellationToken);

        var command = new DeleteUserProfileCommand
        {
            UserId = userId
        };

        await _mediator.Send(command, cancellationToken);
        return NoContent();
    }
}


