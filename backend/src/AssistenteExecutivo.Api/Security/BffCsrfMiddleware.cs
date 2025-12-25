namespace AssistenteExecutivo.Api.Security;

/// <summary>
/// Minimal CSRF protection for the BFF:
/// - Server stores a CSRF token in Session.
/// - Server sets a readable cookie (XSRF-TOKEN) with the same value.
/// - Client must echo the value in header X-CSRF-TOKEN for unsafe methods.
/// </summary>
public sealed class BffCsrfMiddleware
{
    private readonly RequestDelegate _next;

    public BffCsrfMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        // Only protect unsafe HTTP methods.
        if (HttpMethods.IsGet(context.Request.Method) ||
            HttpMethods.IsHead(context.Request.Method) ||
            HttpMethods.IsOptions(context.Request.Method) ||
            HttpMethods.IsTrace(context.Request.Method))
        {
            await _next(context);
            return;
        }

        // Skip swagger endpoints.
        var path = context.Request.Path.Value ?? string.Empty;
        if (path.StartsWith("/swagger", StringComparison.OrdinalIgnoreCase))
        {
            await _next(context);
            return;
        }

        // Skip public auth endpoints that don't require CSRF (register, forgot-password, reset-password)
        if (path.Equals("/auth/register", StringComparison.OrdinalIgnoreCase) ||
            path.Equals("/auth/forgot-password", StringComparison.OrdinalIgnoreCase) ||
            path.Equals("/auth/reset-password", StringComparison.OrdinalIgnoreCase))
        {
            await _next(context);
            return;
        }

        var expected = context.Session.GetString(BffSessionKeys.CsrfToken);
        var cookie = context.Request.Cookies.TryGetValue(BffCsrf.CookieName, out var cookieValue) ? cookieValue : null;
        var header = context.Request.Headers.TryGetValue(BffCsrf.HeaderName, out var headerValue)
            ? headerValue.ToString()
            : null;

        if (string.IsNullOrWhiteSpace(expected) ||
            string.IsNullOrWhiteSpace(cookie) ||
            string.IsNullOrWhiteSpace(header) ||
            !FixedTimeEquals(expected, cookie) ||
            !FixedTimeEquals(expected, header))
        {
            context.Response.StatusCode = StatusCodes.Status403Forbidden;
            await context.Response.WriteAsJsonAsync(new
            {
                error = "csrf_validation_failed",
                message = "CSRF token inválido ou ausente. Envie o cookie XSRF-TOKEN e o header X-CSRF-TOKEN."
            });
            return;
        }

        await _next(context);
    }

    private static bool FixedTimeEquals(string a, string b)
    {
        var aBytes = System.Text.Encoding.UTF8.GetBytes(a);
        var bBytes = System.Text.Encoding.UTF8.GetBytes(b);
        return System.Security.Cryptography.CryptographicOperations.FixedTimeEquals(aBytes, bBytes);
    }
}


