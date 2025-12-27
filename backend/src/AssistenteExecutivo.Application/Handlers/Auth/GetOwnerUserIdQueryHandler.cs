using AssistenteExecutivo.Application.Interfaces;
using AssistenteExecutivo.Application.Queries.Auth;
using MediatR;
using System.IO;

namespace AssistenteExecutivo.Application.Handlers.Auth;

public class GetOwnerUserIdQueryHandler : IRequestHandler<GetOwnerUserIdQuery, Guid?>
{
    private readonly IUserProfileRepository _userProfileRepository;

    public GetOwnerUserIdQueryHandler(IUserProfileRepository userProfileRepository)
    {
        _userProfileRepository = userProfileRepository;
    }

    public async Task<Guid?> Handle(GetOwnerUserIdQuery request, CancellationToken cancellationToken)
    {
        // #region agent log
        var logPath = Path.Combine(Directory.GetCurrentDirectory(), ".cursor", "debug.log");
        try { System.IO.File.AppendAllText(logPath, System.Text.Json.JsonSerializer.Serialize(new { id = $"log_{DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()}_G", timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(), location = "GetOwnerUserIdQueryHandler.Handle", message = "Handler called", data = new { keycloakSubject = request.KeycloakSubject, hasKeycloakSubject = !string.IsNullOrWhiteSpace(request.KeycloakSubject) }, sessionId = "debug-session", runId = "run1", hypothesisId = "G" }) + "\n"); } catch { }
        // #endregion

        if (string.IsNullOrWhiteSpace(request.KeycloakSubject))
        {
            // #region agent log
            try { System.IO.File.AppendAllText(logPath, System.Text.Json.JsonSerializer.Serialize(new { id = $"log_{DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()}_G", timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(), location = "GetOwnerUserIdQueryHandler.Handle", message = "Empty KeycloakSubject, returning null", data = new { }, sessionId = "debug-session", runId = "run1", hypothesisId = "G" }) + "\n"); } catch { }
            // #endregion
            return null;
        }

        try
        {
            // #region agent log
            try { System.IO.File.AppendAllText(logPath, System.Text.Json.JsonSerializer.Serialize(new { id = $"log_{DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()}_G", timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(), location = "GetOwnerUserIdQueryHandler.Handle", message = "Before database query", data = new { keycloakSubjectValue = request.KeycloakSubject }, sessionId = "debug-session", runId = "run1", hypothesisId = "G" }) + "\n"); } catch { }
            // #endregion

            var userProfile = await _userProfileRepository.GetByKeycloakSubjectAsync(request.KeycloakSubject, cancellationToken);

            // #region agent log
            var userId = userProfile?.UserId;
            var status = userProfile?.Status.ToString();
            var email = userProfile?.Email?.Value;
            try { System.IO.File.AppendAllText(logPath, System.Text.Json.JsonSerializer.Serialize(new { id = $"log_{DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()}_G", timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(), location = "GetOwnerUserIdQueryHandler.Handle", message = "After database query", data = new { found = userProfile != null, userId = userId?.ToString(), status = status, email = email }, sessionId = "debug-session", runId = "run1", hypothesisId = "G" }) + "\n"); } catch { }
            // #endregion

            return userProfile?.UserId;
        }
        catch (Exception ex)
        {
            // #region agent log
            try { System.IO.File.AppendAllText(logPath, System.Text.Json.JsonSerializer.Serialize(new { id = $"log_{DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()}_G", timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(), location = "GetOwnerUserIdQueryHandler.Handle", message = "Exception in handler", data = new { exceptionType = ex.GetType().Name, exceptionMessage = ex.Message }, sessionId = "debug-session", runId = "run1", hypothesisId = "G" }) + "\n"); } catch { }
            // #endregion
            // Se KeycloakSubject.Create falhar ou qualquer outro erro, retornar null
            return null;
        }
    }
}

