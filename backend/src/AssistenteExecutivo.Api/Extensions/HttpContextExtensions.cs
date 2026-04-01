using AssistenteExecutivo.Api.Security;
using AssistenteExecutivo.Application.Queries.Auth;
using MediatR;
using System.Security.Claims;

namespace AssistenteExecutivo.Api.Extensions;

public static class HttpContextExtensions
{
    /// <summary>
    /// Obtém o OwnerUserId (UserId) do HttpContext.
    /// Tenta obter do Keycloak subject (sub) via sessão ou JWT token.
    /// </summary>
    public static async Task<Guid?> GetOwnerUserIdAsync(this HttpContext context, IMediator mediator, CancellationToken cancellationToken = default)
    {
        var ownerUserIdFromSession = BffSessionStore.GetOwnerUserId(context.Session);
        if (ownerUserIdFromSession.HasValue)
            return ownerUserIdFromSession.Value;

        string? keycloakSubject = null;
        string? email = null;
        string? displayName = null;

        // Tentar obter da sessão BFF (web)
        var sessionSub = context.Session?.GetString(BffSessionKeys.UserSub);
        if (!string.IsNullOrWhiteSpace(sessionSub))
        {
            keycloakSubject = sessionSub;
        }
        else
        {
            // Tentar obter do JWT token
            keycloakSubject = context.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value
                ?? context.User?.FindFirst("sub")?.Value;

            email = context.User?.FindFirst(ClaimTypes.Email)?.Value
                ?? context.User?.FindFirst("email")?.Value;

            displayName = context.User?.FindFirst(ClaimTypes.Name)?.Value
                ?? context.User?.FindFirst("name")?.Value
                ?? context.User?.FindFirst("full_name")?.Value;
        }

        email ??= context.Request.Headers["X-User-Email"].FirstOrDefault();
        displayName ??= context.Request.Headers["X-User-Name"].FirstOrDefault();

        if (string.IsNullOrWhiteSpace(keycloakSubject))
            return null;

        // Buscar UserProfile pelo KeycloakSubject usando MediatR
        var query = new GetOwnerUserIdQuery
        {
            KeycloakSubject = keycloakSubject,
            Email = email,
            DisplayName = displayName
        };

        return await mediator.Send(query, cancellationToken);
    }

    /// <summary>
    /// Obtém o OwnerUserId (UserId) do HttpContext ou lança exceção se não encontrado.
    /// </summary>
    public static async Task<Guid> GetRequiredOwnerUserIdAsync(this HttpContext context, IMediator mediator, CancellationToken cancellationToken = default)
    {
        var ownerUserId = await context.GetOwnerUserIdAsync(mediator, cancellationToken);
        if (ownerUserId == null)
        {
            throw new UnauthorizedAccessException("Usuário não autenticado ou não encontrado no sistema.");
        }
        return ownerUserId.Value;
    }
}
