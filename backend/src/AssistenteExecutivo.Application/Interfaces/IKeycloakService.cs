namespace AssistenteExecutivo.Application.Interfaces;

public interface IKeycloakService
{
    Task<string> CreateRealmAsync(string realmId, string realmName, bool skipProviders = false, CancellationToken cancellationToken = default);
    Task<string> CreateUserAsync(string realmId, string email, string firstName, string lastName, string password, CancellationToken cancellationToken = default);
    Task AssignRoleAsync(string realmId, string userId, string roleName, CancellationToken cancellationToken = default);
    Task<string> GetAccessTokenAsync(string realmId, string username, string password, CancellationToken cancellationToken = default);
    Task<KeycloakTokenResult> GetTokensAsync(string realmId, string username, string password, CancellationToken cancellationToken = default);
    Task<KeycloakTokenResult> RefreshTokenAsync(string realmId, string refreshToken, CancellationToken cancellationToken = default);
    Task UpdateUserPasswordAsync(string realmId, string userId, string newPassword, CancellationToken cancellationToken = default);
    Task<string> GetSocialLoginUrlAsync(string realmId, string provider, string redirectUri, CancellationToken cancellationToken = default);
    Task<string> GetSocialLoginUrlAsync(string realmId, string provider, string redirectUri, string? state, CancellationToken cancellationToken = default);
    Task<KeycloakTokenResult> ExchangeAuthorizationCodeAsync(string realmId, string code, string redirectUri, CancellationToken cancellationToken = default);
    Task<KeycloakUserInfo> GetUserInfoAsync(string realmId, string accessToken, CancellationToken cancellationToken = default);
    Task LogoutAsync(string realmId, string refreshToken, CancellationToken cancellationToken = default);
    Task ConfigureGoogleIdentityProviderAsync(string realmId, string clientId, string clientSecret, CancellationToken cancellationToken = default);
    Task EnsureClientExistsAsync(string realmId, CancellationToken cancellationToken = default);
    Task ConfigureRealmProvidersAsync(string realmId, CancellationToken cancellationToken = default);
    Task<string?> GetUserIdByEmailAsync(string realmId, string email, CancellationToken cancellationToken = default);
    Task DeleteUserAsync(string realmId, string userId, CancellationToken cancellationToken = default);
    Task<bool> ImportRealmFromJsonAsync(string realmId, string jsonFilePath, bool overwriteExisting = true, CancellationToken cancellationToken = default);
    Task<bool> ImportRealmFromJsonContentAsync(string realmId, string jsonContent, bool overwriteExisting = true, CancellationToken cancellationToken = default);
}

public class KeycloakTokenResult
{
    public string AccessToken { get; set; } = string.Empty;
    public string? RefreshToken { get; set; }
    public int ExpiresIn { get; set; }
}

public class KeycloakUserInfo
{
    public string Sub { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string? GivenName { get; set; }
    public string? FamilyName { get; set; }
    public string? Name { get; set; }
    public string? PreferredUsername { get; set; }
}

