using System.Net.Http.Json;
using AssistenteExecutivo.Application.Interfaces;
using AssistenteExecutivo.Domain.Entities;
using AssistenteExecutivo.Domain.Interfaces;
using AssistenteExecutivo.Domain.ValueObjects;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.EntityFrameworkCore;
using AssistenteExecutivo.Infrastructure.Persistence;

namespace AssistenteExecutivo.Infrastructure.Services;

/// <summary>
/// Hosted service que provisiona o Keycloak automaticamente no startup (apenas DEV/HML).
/// </summary>
public class KeycloakAdminProvisioner : IKeycloakAdminProvisioner, IHostedService
{
    private readonly IKeycloakService _keycloakService;
    private readonly IConfiguration _configuration;
    private readonly ILogger<KeycloakAdminProvisioner> _logger;
    private readonly IHostEnvironment _environment;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly IClock _clock;

    public KeycloakAdminProvisioner(
        IKeycloakService keycloakService,
        IConfiguration configuration,
        ILogger<KeycloakAdminProvisioner> logger,
        IHostEnvironment environment,
        IServiceScopeFactory scopeFactory,
        IClock clock)
    {
        _keycloakService = keycloakService;
        _configuration = configuration;
        _logger = logger;
        _environment = environment;
        _scopeFactory = scopeFactory;
        _clock = clock;
    }

    public async Task ProvisionAsync(CancellationToken cancellationToken = default)
    {
        // Executar apenas em DEV/HML
        if (_environment.IsProduction())
        {
            _logger.LogInformation("KeycloakAdminProvisioner: Pulando provisionamento em ambiente de produção");
            return;
        }

        try
        {
            _logger.LogInformation("Iniciando provisionamento do Keycloak...");

            var realmId = _configuration["Keycloak:Realm"] ?? "assistenteexecutivo";
            var realmName = _configuration["Keycloak:RealmName"] ?? "Assistente Executivo";

            // Criar/atualizar realm
            await _keycloakService.CreateRealmAsync(realmId, realmName, cancellationToken);
            _logger.LogInformation("Realm {RealmId} garantido", realmId);

            // Garantir que os clients existem
            await _keycloakService.EnsureClientExistsAsync(realmId, cancellationToken);
            _logger.LogInformation("Clients garantidos no realm {RealmId}", realmId);

            // Criar roles básicas
            await EnsureRolesAsync(realmId, cancellationToken);
            _logger.LogInformation("Roles garantidas no realm {RealmId}", realmId);

            // Criar usuários dev/teste
            await CreateDevTestUsersAsync(realmId, cancellationToken);
            _logger.LogInformation("Usuários dev/teste garantidos no realm {RealmId}", realmId);

            // Configurar Google IdP se credenciais estiverem presentes
            var googleClientId = _configuration["Keycloak:Google:ClientId"];
            var googleClientSecret = _configuration["Keycloak:Google:ClientSecret"];
            if (!string.IsNullOrWhiteSpace(googleClientId) && !string.IsNullOrWhiteSpace(googleClientSecret))
            {
                await _keycloakService.ConfigureGoogleIdentityProviderAsync(realmId, googleClientId, googleClientSecret, cancellationToken);
                _logger.LogInformation("Google Identity Provider configurado no realm {RealmId}", realmId);
            }
            else
            {
                _logger.LogWarning("Credenciais do Google não encontradas. Google IdP não será configurado. " +
                    "Configure Keycloak:Google:ClientId e Keycloak:Google:ClientSecret em appsettings para habilitar login com Google.");
            }

            _logger.LogInformation("Provisionamento do Keycloak concluído com sucesso");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro durante o provisionamento do Keycloak");
            throw;
        }
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        await ProvisionAsync(cancellationToken);
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }

    private async Task EnsureRolesAsync(string realmId, CancellationToken cancellationToken)
    {
        var roles = new[] { "admin", "user", "viewer" };

        foreach (var roleName in roles)
        {
            try
            {
                // Tentar atribuir a role a um usuário dummy para verificar se existe
                // Se não existir, criar
                var adminToken = await GetAdminTokenAsync(cancellationToken);
                var checkRequest = new System.Net.Http.HttpRequestMessage(
                    System.Net.Http.HttpMethod.Get,
                    $"{_configuration["Keycloak:BaseUrl"] ?? throw new InvalidOperationException("Keycloak:BaseUrl não configurado em appsettings")}/admin/realms/{realmId}/roles/{roleName}");
                checkRequest.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", adminToken);

                using var httpClient = new System.Net.Http.HttpClient();
                var checkResponse = await httpClient.SendAsync(checkRequest, cancellationToken);
                
                if (!checkResponse.IsSuccessStatusCode)
                {
                    // Role não existe, criar
                    var createRequest = new System.Net.Http.HttpRequestMessage(
                        System.Net.Http.HttpMethod.Post,
                        $"{_configuration["Keycloak:BaseUrl"] ?? throw new InvalidOperationException("Keycloak:BaseUrl não configurado em appsettings")}/admin/realms/{realmId}/roles");
                    createRequest.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", adminToken);
                    createRequest.Content = System.Net.Http.Json.JsonContent.Create(new { name = roleName });

                    var createResponse = await httpClient.SendAsync(createRequest, cancellationToken);
                    if (createResponse.IsSuccessStatusCode)
                    {
                        _logger.LogInformation("Role {RoleName} criada no realm {RealmId}", roleName, realmId);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Erro ao garantir role {RoleName} no realm {RealmId}", roleName, realmId);
            }
        }
    }

    private async Task CreateDevTestUsersAsync(string realmId, CancellationToken cancellationToken)
    {
        var devUsers = new[]
        {
            new { Email = "admin@assistenteexecutivo.local", FirstName = "Admin", LastName = "Sistema", Password = "Admin@123", Role = "admin" },
            new { Email = "user@assistenteexecutivo.local", FirstName = "Usuário", LastName = "Teste", Password = "User@123", Role = "user" },
            new { Email = "viewer@assistenteexecutivo.local", FirstName = "Visualizador", LastName = "Teste", Password = "Viewer@123", Role = "viewer" }
        };

        foreach (var user in devUsers)
        {
            try
            {
                // Verificar se usuário já existe
                var existingUserId = await _keycloakService.GetUserIdByEmailAsync(realmId, user.Email, cancellationToken);
                
                if (string.IsNullOrEmpty(existingUserId))
                {
                    // Criar usuário
                    var userId = await _keycloakService.CreateUserAsync(
                        realmId,
                        user.Email,
                        user.FirstName,
                        user.LastName,
                        user.Password,
                        cancellationToken);

                    // Atribuir role
                    await _keycloakService.AssignRoleAsync(realmId, userId, user.Role, cancellationToken);
                    
                    _logger.LogInformation("Usuário dev/teste criado: {Email} (role: {Role})", user.Email, user.Role);

                    // Garantir perfil local (para fluxo de forgot/reset-password via EmailService)
                    await EnsureLocalUserProfileAsync(user.Email, user.FirstName, user.LastName, userId, cancellationToken);
                }
                else
                {
                    // Usuário já existe, apenas garantir que tem a role
                    try
                    {
                        await _keycloakService.AssignRoleAsync(realmId, existingUserId, user.Role, cancellationToken);
                        _logger.LogInformation("Usuário dev/teste já existe: {Email} (role: {Role})", user.Email, user.Role);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "Erro ao atribuir role {Role} ao usuário {Email}", user.Role, user.Email);
                    }

                    // Garantir perfil local (para fluxo de forgot/reset-password via EmailService)
                    await EnsureLocalUserProfileAsync(user.Email, user.FirstName, user.LastName, existingUserId, cancellationToken);
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Erro ao criar/atualizar usuário dev/teste {Email}: {Error}", user.Email, ex.Message);
            }
        }
    }

    private async Task EnsureLocalUserProfileAsync(
        string email,
        string firstName,
        string lastName,
        string keycloakUserId,
        CancellationToken cancellationToken)
    {
        try
        {
            using var scope = _scopeFactory.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

            var normalizedEmail = EmailAddress.Create(email).Value;
            var existing = await db.UserProfiles
                .FirstOrDefaultAsync(u => u.Email.Value == normalizedEmail, cancellationToken);

            var subject = KeycloakSubject.Create(keycloakUserId);
            var personName = PersonName.Create(firstName, lastName);
            var emailVo = EmailAddress.Create(email);

            if (existing == null)
            {
                var profile = new UserProfile(
                    userId: Guid.NewGuid(),
                    keycloakSubject: subject,
                    email: emailVo,
                    displayName: personName,
                    clock: _clock);

                db.UserProfiles.Add(profile);
                await db.SaveChangesAsync(cancellationToken);

                _logger.LogInformation("UserProfile local criado para {Email} (KeycloakSubject={KeycloakSubject})", emailVo.Value, subject.Value);
                return;
            }

            // Se já existe, garantir dados (sem trocar subject)
            if (existing.KeycloakSubject != subject)
            {
                _logger.LogWarning(
                    "UserProfile local para {Email} já existe com outro KeycloakSubject ({ExistingSubject}). Não será alterado para {NewSubject}.",
                    emailVo.Value,
                    existing.KeycloakSubject.Value,
                    subject.Value);
                return;
            }

            existing.ProvisionFromKeycloak(subject, emailVo, personName, _clock);
            await db.SaveChangesAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Falha ao garantir UserProfile local para {Email}", email);
        }
    }

    private async Task<string> GetAdminTokenAsync(CancellationToken cancellationToken)
    {
        var keycloakBaseUrl = _configuration["Keycloak:BaseUrl"] 
            ?? throw new InvalidOperationException("Keycloak:BaseUrl não configurado em appsettings");
        var adminRealm = _configuration["Keycloak:AdminRealm"] ?? "master";
        var adminClientId = _configuration["Keycloak:AdminClientId"] ?? "admin-cli";
        var adminUsername = _configuration["Keycloak:AdminUsername"] ?? "admin";
        var adminPassword = _configuration["Keycloak:AdminPassword"] ?? "admin";

        var tokenRequest = new Dictionary<string, string>
        {
            { "grant_type", "password" },
            { "client_id", adminClientId },
            { "username", adminUsername },
            { "password", adminPassword }
        };

        using var httpClient = new System.Net.Http.HttpClient();
        var request = new System.Net.Http.HttpRequestMessage(
            System.Net.Http.HttpMethod.Post,
            $"{keycloakBaseUrl}/realms/{adminRealm}/protocol/openid-connect/token")
        {
            Content = new System.Net.Http.FormUrlEncodedContent(tokenRequest)
        };

        var response = await httpClient.SendAsync(request, cancellationToken);
        response.EnsureSuccessStatusCode();

        var tokenResponse = await response.Content.ReadFromJsonAsync<KeycloakTokenResponse>(cancellationToken: cancellationToken);
        if (tokenResponse == null || string.IsNullOrWhiteSpace(tokenResponse.AccessToken))
            throw new Exception("Token de admin não retornado");

        return tokenResponse.AccessToken;
    }

    private class KeycloakTokenResponse
    {
        public string AccessToken { get; set; } = string.Empty;
        public int ExpiresIn { get; set; }
    }
}

