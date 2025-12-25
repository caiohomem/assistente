using System.Security.Cryptography;

namespace AssistenteExecutivo.Api.Security;

public static class BffCsrf
{
    public const string CookieName = "XSRF-TOKEN";
    public const string HeaderName = "X-CSRF-TOKEN";

    public static string EnsureToken(ISession session)
    {
        var existing = session.GetString(BffSessionKeys.CsrfToken);
        if (!string.IsNullOrWhiteSpace(existing))
            return existing;

        var token = GenerateToken();
        session.SetString(BffSessionKeys.CsrfToken, token);
        return token;
    }

    public static void RotateToken(ISession session)
    {
        var token = GenerateToken();
        session.SetString(BffSessionKeys.CsrfToken, token);
    }

    public static string GenerateToken()
    {
        // 32 bytes -> 256 bits, URL-safe
        return Base64UrlEncode(RandomNumberGenerator.GetBytes(32));
    }

    private static string Base64UrlEncode(byte[] bytes)
    {
        return Convert.ToBase64String(bytes)
            .TrimEnd('=')
            .Replace('+', '-')
            .Replace('/', '_');
    }
}


