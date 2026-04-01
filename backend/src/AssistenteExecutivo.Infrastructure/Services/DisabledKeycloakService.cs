using AssistenteExecutivo.Application.Interfaces;

namespace AssistenteExecutivo.Infrastructure.Services;

public class DisabledKeycloakService : IKeycloakService
{
    private static NotSupportedException Disabled() =>
        new("Keycloak foi desabilitado neste ambiente. Use Clerk como provedor de autenticacao.");

    public Task<string> CreateRealmAsync(string realmId, string realmName, bool skipProviders = false, CancellationToken cancellationToken = default) => throw Disabled();
    public Task<string> CreateUserAsync(string realmId, string email, string firstName, string lastName, string password, CancellationToken cancellationToken = default) => throw Disabled();
    public Task AssignRoleAsync(string realmId, string userId, string roleName, CancellationToken cancellationToken = default) => throw Disabled();
    public Task<string> GetAccessTokenAsync(string realmId, string username, string password, CancellationToken cancellationToken = default) => throw Disabled();
    public Task<KeycloakTokenResult> GetTokensAsync(string realmId, string username, string password, CancellationToken cancellationToken = default) => throw Disabled();
    public Task<KeycloakTokenResult> RefreshTokenAsync(string realmId, string refreshToken, CancellationToken cancellationToken = default) => throw Disabled();
    public Task UpdateUserPasswordAsync(string realmId, string userId, string newPassword, CancellationToken cancellationToken = default) => throw Disabled();
    public Task<string> GetSocialLoginUrlAsync(string realmId, string provider, string redirectUri, CancellationToken cancellationToken = default) => throw Disabled();
    public Task<string> GetSocialLoginUrlAsync(string realmId, string provider, string redirectUri, string? state, CancellationToken cancellationToken = default) => throw Disabled();
    public Task<KeycloakTokenResult> ExchangeAuthorizationCodeAsync(string realmId, string code, string redirectUri, CancellationToken cancellationToken = default) => throw Disabled();
    public Task<KeycloakUserInfo> GetUserInfoAsync(string realmId, string accessToken, CancellationToken cancellationToken = default) => throw Disabled();
    public Task LogoutAsync(string realmId, string refreshToken, CancellationToken cancellationToken = default) => throw Disabled();
    public Task ConfigureGoogleIdentityProviderAsync(string realmId, string clientId, string clientSecret, CancellationToken cancellationToken = default) => throw Disabled();
    public Task EnsureClientExistsAsync(string realmId, CancellationToken cancellationToken = default) => throw Disabled();
    public Task ConfigureRealmProvidersAsync(string realmId, CancellationToken cancellationToken = default) => throw Disabled();
    public Task<string?> GetUserIdByEmailAsync(string realmId, string email, CancellationToken cancellationToken = default) => throw Disabled();
    public Task DeleteUserAsync(string realmId, string userId, CancellationToken cancellationToken = default) => Task.CompletedTask;
    public Task<bool> ImportRealmFromJsonAsync(string realmId, string jsonFilePath, bool overwriteExisting = true, CancellationToken cancellationToken = default) => Task.FromResult(false);
    public Task<bool> ImportRealmFromJsonContentAsync(string realmId, string jsonContent, bool overwriteExisting = true, CancellationToken cancellationToken = default) => Task.FromResult(false);
}
