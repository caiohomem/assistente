namespace AssistenteExecutivo.Api.Security;

public static class BffSessionKeys
{
    public const string OAuthState = "auth.oauth.state";
    public const string ReturnPath = "auth.return.path";
    public const string Action = "auth.action"; // "login" or "register"
    public const string FrontendBaseUrl = "auth.frontend.baseUrl";

    public const string IsAuthenticated = "auth.isAuthenticated";

    public const string AccessToken = "auth.kc.accessToken";
    public const string RefreshToken = "auth.kc.refreshToken";
    public const string ExpiresAtUnix = "auth.kc.expiresAtUnix";

    public const string UserSub = "auth.user.sub";
    public const string OwnerUserId = "auth.user.userId";
    public const string UserEmail = "auth.user.email";
    public const string UserName = "auth.user.name";
    public const string UserGivenName = "auth.user.givenName";
    public const string UserFamilyName = "auth.user.familyName";

    public const string CsrfToken = "csrf.token";
}


