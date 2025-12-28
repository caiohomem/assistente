using AssistenteExecutivo.Api.Security;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Options;
using System.Security.Claims;
using System.Text.Encodings.Web;
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
        // Session middleware must run before authentication for this to work.
        var isAuthenticated = BffSessionStore.IsAuthenticated(Context.Session);

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

        return Task.FromResult(AuthenticateResult.Success(ticket));
    }
}


