using System.Security.Claims;
using System.Text.Encodings.Web;
using AssistenteExecutivo.Api.Security;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Options;
using System.IO;

namespace AssistenteExecutivo.Api.Auth;

public sealed class BffSessionAuthenticationHandler : AuthenticationHandler<AuthenticationSchemeOptions>
{
    public BffSessionAuthenticationHandler(
        IOptionsMonitor<AuthenticationSchemeOptions> options,
        ILoggerFactory logger,
        UrlEncoder encoder)
        : base(options, logger, encoder)
    {
    }

    protected override Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        // #region agent log
        var logPath = Path.Combine(Directory.GetCurrentDirectory(), ".cursor", "debug.log");
        var cookieHeader = Context.Request.Headers["Cookie"].ToString();
        var sessionId = Context.Session?.Id ?? "null";
        var hasSessionCookie = cookieHeader.Contains("ae.sid", StringComparison.OrdinalIgnoreCase) || Context.Request.Cookies.ContainsKey("ae.sid");
        try { System.IO.File.AppendAllText(logPath, System.Text.Json.JsonSerializer.Serialize(new { id = $"log_{DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()}_F", timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(), location = "BffSessionAuthenticationHandler.HandleAuthenticateAsync", message = "Authentication handler called", data = new { sessionId = sessionId, hasSessionCookie = hasSessionCookie, path = Context.Request.Path.ToString(), method = Context.Request.Method, cookieHeaderLength = cookieHeader?.Length ?? 0 }, sessionId = "debug-session", runId = "run1", hypothesisId = "F" }) + "\n"); } catch { }
        // #endregion

        // Session middleware must run before authentication for this to work.
        var isAuthenticated = BffSessionStore.IsAuthenticated(Context.Session);
        
        // #region agent log
        var hasUserSub = !string.IsNullOrWhiteSpace(Context.Session.GetString(BffSessionKeys.UserSub));
        var hasUserEmail = !string.IsNullOrWhiteSpace(Context.Session.GetString(BffSessionKeys.UserEmail));
        try { System.IO.File.AppendAllText(logPath, System.Text.Json.JsonSerializer.Serialize(new { id = $"log_{DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()}_F", timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(), location = "BffSessionAuthenticationHandler.HandleAuthenticateAsync", message = "Session authentication check", data = new { sessionId = sessionId, isAuthenticated = isAuthenticated, hasUserSub = hasUserSub, hasUserEmail = hasUserEmail, hasSessionCookie = hasSessionCookie }, sessionId = "debug-session", runId = "run1", hypothesisId = "F" }) + "\n"); } catch { }
        // #endregion

        if (!isAuthenticated)
            return Task.FromResult(AuthenticateResult.NoResult());

        var sub = Context.Session.GetString(BffSessionKeys.UserSub) ?? string.Empty;
        var email = Context.Session.GetString(BffSessionKeys.UserEmail) ?? string.Empty;
        var name = Context.Session.GetString(BffSessionKeys.UserName) ?? string.Empty;

        var claims = new List<Claim>();
        if (!string.IsNullOrWhiteSpace(sub)) claims.Add(new Claim(ClaimTypes.NameIdentifier, sub));
        if (!string.IsNullOrWhiteSpace(email)) claims.Add(new Claim(ClaimTypes.Email, email));
        if (!string.IsNullOrWhiteSpace(name)) claims.Add(new Claim(ClaimTypes.Name, name));

        var identity = new ClaimsIdentity(claims, BffSessionAuthenticationDefaults.Scheme);
        var principal = new ClaimsPrincipal(identity);
        var ticket = new AuthenticationTicket(principal, BffSessionAuthenticationDefaults.Scheme);

        // #region agent log
        try { System.IO.File.AppendAllText(logPath, System.Text.Json.JsonSerializer.Serialize(new { id = $"log_{DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()}_F", timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(), location = "BffSessionAuthenticationHandler.HandleAuthenticateAsync", message = "Authentication success", data = new { sessionId = sessionId, claimsCount = claims.Count, sub = sub, email = email }, sessionId = "debug-session", runId = "run1", hypothesisId = "F" }) + "\n"); } catch { }
        // #endregion

        return Task.FromResult(AuthenticateResult.Success(ticket));
    }
}


