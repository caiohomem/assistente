using AssistenteExecutivo.Application.Interfaces;

namespace AssistenteExecutivo.Api.Security;

public static class BffSessionStore
{
    public static bool IsAuthenticated(ISession session)
    {
        return string.Equals(session.GetString(BffSessionKeys.IsAuthenticated), "1", StringComparison.Ordinal);
    }

    public static void SetAuthenticated(ISession session, bool value)
    {
        session.SetString(BffSessionKeys.IsAuthenticated, value ? "1" : "0");
    }

    public static void StoreOwnerUserId(ISession session, Guid userId)
    {
        session.SetString(BffSessionKeys.OwnerUserId, userId.ToString());
    }

    public static Guid? GetOwnerUserId(ISession session)
    {
        var raw = session.GetString(BffSessionKeys.OwnerUserId);
        return Guid.TryParse(raw, out var userId) ? userId : null;
    }

    public static void StoreTokens(ISession session, KeycloakTokenResult tokens)
    {
        session.SetString(BffSessionKeys.AccessToken, tokens.AccessToken);
        if (!string.IsNullOrWhiteSpace(tokens.RefreshToken))
            session.SetString(BffSessionKeys.RefreshToken, tokens.RefreshToken);

        var expiresAt = DateTimeOffset.UtcNow.AddSeconds(tokens.ExpiresIn);
        session.SetString(BffSessionKeys.ExpiresAtUnix, expiresAt.ToUnixTimeSeconds().ToString());
    }

    public static long? GetExpiresAtUnix(ISession session)
    {
        var raw = session.GetString(BffSessionKeys.ExpiresAtUnix);
        return long.TryParse(raw, out var unix) ? unix : null;
    }

    public static void StoreUser(ISession session, KeycloakUserInfo userInfo)
    {
        session.SetString(BffSessionKeys.UserSub, userInfo.Sub);
        session.SetString(BffSessionKeys.UserEmail, userInfo.Email);

        if (!string.IsNullOrWhiteSpace(userInfo.Name))
            session.SetString(BffSessionKeys.UserName, userInfo.Name);
        else if (!string.IsNullOrWhiteSpace(userInfo.PreferredUsername))
            session.SetString(BffSessionKeys.UserName, userInfo.PreferredUsername);

        if (!string.IsNullOrWhiteSpace(userInfo.GivenName))
            session.SetString(BffSessionKeys.UserGivenName, userInfo.GivenName);
        if (!string.IsNullOrWhiteSpace(userInfo.FamilyName))
            session.SetString(BffSessionKeys.UserFamilyName, userInfo.FamilyName);
    }
}


