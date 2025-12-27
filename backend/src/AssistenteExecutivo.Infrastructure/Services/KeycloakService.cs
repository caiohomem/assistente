using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using AssistenteExecutivo.Application.Interfaces;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace AssistenteExecutivo.Infrastructure.Services;

public class KeycloakService : IKeycloakService
{
    private readonly HttpClient _httpClient;
    private readonly IConfiguration _configuration;
    private readonly ILogger<KeycloakService> _logger;
    private readonly IMemoryCache _memoryCache;
    private readonly SemaphoreSlim _tokenSemaphore;
    private readonly string _keycloakBaseUrl;
    private readonly string _adminRealm;
    private readonly string _adminClientId;
    private readonly string _adminClientSecret;
    private readonly string _adminUsername;
    private readonly string _adminPassword;
    private readonly string _clientId;
    private readonly string _clientSecret;
    private const string AdminTokenCacheKey = "keycloak_admin_token";
    private const string MobileRedirectUri1 = "com.assistenteexecutivo.app:/oauthredirect";
    private const string MobileRedirectUri2 = "com.assistenteexecutivo.app://oauthredirect";
    private const string MobileRedirectUri3 = "com.assistenteexecutivo.app://oauth/callback";

    public KeycloakService(
        HttpClient httpClient,
        IConfiguration configuration,
        ILogger<KeycloakService> logger,
        IMemoryCache memoryCache)
    {
        _httpClient = httpClient;
        _configuration = configuration;
        _logger = logger;
        _memoryCache = memoryCache;
        _tokenSemaphore = new SemaphoreSlim(1, 1);
        _keycloakBaseUrl = _configuration["Keycloak:BaseUrl"] 
            ?? throw new InvalidOperationException("Keycloak:BaseUrl não configurado em appsettings");
        _adminRealm = _configuration["Keycloak:AdminRealm"] ?? "master";
        _adminClientId = _configuration["Keycloak:AdminClientId"] ?? "admin-cli";
        _adminClientSecret = _configuration["Keycloak:AdminClientSecret"] ?? "";
        _adminUsername = _configuration["Keycloak:AdminUsername"] ?? "admin";
        _adminPassword = _configuration["Keycloak:AdminPassword"] ?? "admin";
        _clientId = _configuration["Keycloak:ClientId"] ?? "assistenteexecutivo-app";
        _clientSecret = _configuration["Keycloak:ClientSecret"] ?? "";
    }

    public async Task<string> CreateRealmAsync(string realmId, string realmName, CancellationToken cancellationToken = default)
    {
        try
        {
            var adminToken = await GetAdminTokenAsync(cancellationToken);

            // Verificar se o realm já existe
            var checkRequest = new HttpRequestMessage(HttpMethod.Get, $"{_keycloakBaseUrl}/admin/realms/{realmId}");
            checkRequest.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", adminToken);
            
            var checkResponse = await _httpClient.SendAsync(checkRequest, cancellationToken);
            if (checkResponse.IsSuccessStatusCode)
            {
                _logger.LogInformation("Realm {RealmId} já existe. Atualizando configurações...", realmId);
                // Realm já existe, continuar para garantir que clients/roles estão configurados
            }
            else
            {
                // Criar realm (frontendUrl será configurado depois via UpdateRealmFrontendUrlAsync)
                // O Keycloak não aceita frontendUrl no payload de criação inicial
                var realmRequest = new
                {
                    realm = realmId,
                    enabled = true,
                    displayName = realmName
                };

                var request = new HttpRequestMessage(HttpMethod.Post, $"{_keycloakBaseUrl}/admin/realms");
                request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", adminToken);
                request.Content = JsonContent.Create(realmRequest);

                var response = await _httpClient.SendAsync(request, cancellationToken);
                
                if (!response.IsSuccessStatusCode)
                {
                    var errorContent = await response.Content.ReadAsStringAsync(cancellationToken);
                    _logger.LogError("Erro ao criar realm {RealmId}. Status: {Status}, Response: {Response}", 
                        realmId, response.StatusCode, errorContent);
                    response.EnsureSuccessStatusCode();
                }

                _logger.LogInformation("Realm {RealmId} criado com sucesso", realmId);
            }

            // Configurar Frontend URL para garantir que tokens usem HTTPS no issuer
            // IMPORTANTE: Deve ser feito ANTES de configurar Identity Providers
            // para que o redirect URI seja calculado corretamente
            // (Atualiza mesmo se o realm já existir, para garantir que está correto)
            await UpdateRealmFrontendUrlAsync(realmId, cancellationToken);
            await ConfigureRealmLoginSettingsAsync(realmId, cancellationToken);

            // Criar/atualizar clients
            try
            {
                await EnsureClientExistsAsync(realmId, cancellationToken);
                _logger.LogInformation("Clients garantidos no realm {RealmId}", realmId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao criar/atualizar clients no realm {RealmId}", realmId);
            }

            // Configurar Google Identity Provider (após frontendUrl ser atualizado)
            try
            {
                await ConfigureRealmProvidersAsync(realmId, cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Erro ao configurar providers no realm {RealmId}", realmId);
            }

            // Configurar theme customizado
            try
            {
                var themeName = _configuration["Keycloak:ThemeName"] ?? "assistenteexecutivo";
                var applied = true;
                await ConfigureRealmThemeAsync(realmId, themeName, cancellationToken);
                if (applied)
                {
                    _logger.LogInformation("Theme {ThemeName} configurado no realm {RealmId}", themeName, realmId);
                }
                else
                {
                    _logger.LogWarning("Theme {ThemeName} nAœo foi aplicado no realm {RealmId}. Verifique se o theme existe no servidor do Keycloak.", themeName, realmId);
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Erro ao configurar theme no realm {RealmId}. O realm foi criado mas o theme padrão será usado.", realmId);
            }

            return realmId;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao criar realm {RealmId}", realmId);
            throw;
        }
    }

    private async Task UpdateRealmFrontendUrlAsync(string realmId, CancellationToken cancellationToken)
    {
        try
        {
            var adminToken = await GetAdminTokenAsync(cancellationToken);

            // O theme em si nAœo pode ser "provisionado" via Admin API: ele precisa existir no filesystem do Keycloak
            // (ex: /opt/keycloak/themes/<themeName> no container).
            var themeName = _configuration["Keycloak:ThemeName"] ?? "assistenteexecutivo";
            if (false)
            {
                _logger.LogWarning(
                    "Theme {ThemeName} nAœo encontrado no Keycloak. Em Docker, garanta que exista em /opt/keycloak/themes/{ThemeName}.",
                    themeName);
                return;
            }
            
            // Get current realm configuration
            var getRequest = new HttpRequestMessage(HttpMethod.Get, $"{_keycloakBaseUrl}/admin/realms/{realmId}");
            getRequest.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", adminToken);
            
            var getResponse = await _httpClient.SendAsync(getRequest, cancellationToken);
            if (!getResponse.IsSuccessStatusCode)
            {
                _logger.LogWarning("Não foi possível obter configuração do realm {RealmId} para atualizar Frontend URL", realmId);
                return;
            }

            // Ler o JSON como string primeiro para poder reutilizar
            var realmJson = await getResponse.Content.ReadAsStringAsync(cancellationToken);
            var realmConfig = JsonSerializer.Deserialize<JsonElement>(realmJson);
            
            // Frontend URL do realm:
            // - Em DEV (Keycloak local), manter vazio para não forçar redirects para o hostname externo.
            // - Atrás de proxy HTTPS, o Keycloak consegue gerar URLs https corretamente desde que X-Forwarded-* esteja ok.
            var frontendUrl = _keycloakBaseUrl.TrimEnd('/');

            // Se o Keycloak está local (localhost), limpar o frontendUrl
            if (Uri.TryCreate(_keycloakBaseUrl, UriKind.Absolute, out var baseUri)
                && (string.Equals(baseUri.Host, "localhost", StringComparison.OrdinalIgnoreCase)
                    || string.Equals(baseUri.Host, "127.0.0.1", StringComparison.OrdinalIgnoreCase)
                    || string.Equals(baseUri.Host, "::1", StringComparison.OrdinalIgnoreCase)))
            {
                frontendUrl = string.Empty;
            }

            // Verificar se o frontendUrl já está correto
            var currentFrontendUrl = realmConfig.TryGetProperty("frontendUrl", out var frontendUrlProp) 
                ? frontendUrlProp.GetString() 
                : null;
            
            if (currentFrontendUrl == frontendUrl)
            {
                _logger.LogInformation("Realm {RealmId} já possui frontendUrl correto: {FrontendUrl}", realmId, frontendUrl);
                return;
            }

            // Update realm with frontendUrl
            // Usar o realmJson já lido acima para criar payload de atualização
            var jsonOptions = new JsonSerializerOptions { WriteIndented = false };
            var realmDict = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(realmJson, jsonOptions) 
                ?? new Dictionary<string, JsonElement>();
            
            // Criar payload de atualização com todas as propriedades do realm
            var updatePayload = new Dictionary<string, object>();
            foreach (var kvp in realmDict)
            {
                if (kvp.Key == "id")
                {
                    continue; // Não incluir id no update
                }
                
                // Converter JsonElement para object
                object? value = kvp.Value.ValueKind switch
                {
                    JsonValueKind.String => kvp.Value.GetString() ?? string.Empty,
                    JsonValueKind.Number => kvp.Value.TryGetInt32(out var intVal) ? intVal : (object)kvp.Value.GetDouble(),
                    JsonValueKind.True => true,
                    JsonValueKind.False => false,
                    JsonValueKind.Null => null,
                    _ => kvp.Value
                };
                
                if (value != null)
                {
                    updatePayload[kvp.Key] = value;
                }
            }
            
            // Atualizar frontendUrl
            updatePayload["frontendUrl"] = frontendUrl;

            var updateRequest = new HttpRequestMessage(HttpMethod.Put, $"{_keycloakBaseUrl}/admin/realms/{realmId}");
            updateRequest.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", adminToken);
            updateRequest.Content = JsonContent.Create(updatePayload);

            var updateResponse = await _httpClient.SendAsync(updateRequest, cancellationToken);
            if (updateResponse.IsSuccessStatusCode)
            {
                _logger.LogInformation("Realm {RealmId} atualizado com frontendUrl: {FrontendUrl}", realmId, frontendUrl);
            }
            else
            {
                var errorContent = await updateResponse.Content.ReadAsStringAsync(cancellationToken);
                _logger.LogWarning("Erro ao atualizar frontendUrl do realm {RealmId}. Status: {Status}, Response: {Response}", 
                    realmId, updateResponse.StatusCode, errorContent);
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Erro ao atualizar frontendUrl do realm {RealmId}", realmId);
            // Não lançar exceção, apenas logar - não é crítico para o funcionamento
        }
    }

    private async Task ConfigureRealmLoginSettingsAsync(string realmId, CancellationToken cancellationToken)
    {
        try
        {
            var adminToken = await GetAdminTokenAsync(cancellationToken);

            var getRequest = new HttpRequestMessage(HttpMethod.Get, $"{_keycloakBaseUrl}/admin/realms/{realmId}");
            getRequest.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", adminToken);

            var getResponse = await _httpClient.SendAsync(getRequest, cancellationToken);
            if (!getResponse.IsSuccessStatusCode)
            {
                _logger.LogWarning("NAœo foi possA-vel obter configuraAAœo do realm {RealmId} para configurar login settings", realmId);
                return;
            }

            var realmJson = await getResponse.Content.ReadAsStringAsync(cancellationToken);
            var jsonOptions = new JsonSerializerOptions { WriteIndented = false };
            var realmDict = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(realmJson, jsonOptions)
                ?? new Dictionary<string, JsonElement>();

            var updatePayload = new Dictionary<string, object>();
            foreach (var kvp in realmDict)
            {
                if (kvp.Key == "id")
                    continue;

                object? value = kvp.Value.ValueKind switch
                {
                    JsonValueKind.String => kvp.Value.GetString() ?? string.Empty,
                    JsonValueKind.Number => kvp.Value.TryGetInt32(out var intVal) ? intVal : (object)kvp.Value.GetDouble(),
                    JsonValueKind.True => true,
                    JsonValueKind.False => false,
                    JsonValueKind.Null => null,
                    _ => kvp.Value
                };

                if (value != null)
                    updatePayload[kvp.Key] = value;
            }

            updatePayload["registrationAllowed"] = true;
            updatePayload["resetPasswordAllowed"] = true;
            updatePayload["rememberMe"] = false;

            var updateRequest = new HttpRequestMessage(HttpMethod.Put, $"{_keycloakBaseUrl}/admin/realms/{realmId}");
            updateRequest.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", adminToken);
            updateRequest.Content = JsonContent.Create(updatePayload);

            var updateResponse = await _httpClient.SendAsync(updateRequest, cancellationToken);
            if (updateResponse.IsSuccessStatusCode)
            {
                _logger.LogInformation("Login settings configurados no realm {RealmId}", realmId);
            }
            else
            {
                var errorContent = await updateResponse.Content.ReadAsStringAsync(cancellationToken);
                _logger.LogWarning("Erro ao configurar login settings no realm {RealmId}. Status: {Status}, Response: {Response}",
                    realmId, updateResponse.StatusCode, errorContent);
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Erro ao configurar login settings no realm {RealmId}", realmId);
        }
    }

    public async Task<string> CreateUserAsync(string realmId, string email, string firstName, string lastName, string password, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(email))
            throw new ArgumentException("Email não pode ser vazio", nameof(email));

        try
        {
            var adminToken = await GetAdminTokenAsync(cancellationToken);

            // Verificar se o usuário já existe
            var existingUserId = await GetUserIdByEmailAsync(realmId, email, cancellationToken);
            if (!string.IsNullOrEmpty(existingUserId))
            {
                _logger.LogInformation("Usuário {Email} já existe no realm {RealmId} com ID {UserId}", email, realmId, existingUserId);
                return existingUserId;
            }

            var userRequest = new Dictionary<string, object>
            {
                { "username", email },
                { "email", email },
                { "firstName", firstName },
                { "lastName", lastName },
                { "enabled", true },
                { "emailVerified", true }
            };

            if (!string.IsNullOrWhiteSpace(password))
            {
                userRequest["credentials"] = new[]
                {
                    new
                    {
                        type = "password",
                        value = password,
                        temporary = false
                    }
                };
            }

            var request = new HttpRequestMessage(HttpMethod.Post, $"{_keycloakBaseUrl}/admin/realms/{realmId}/users");
            request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", adminToken);
            request.Content = JsonContent.Create(userRequest);

            var response = await _httpClient.SendAsync(request, cancellationToken);
            
            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await response.Content.ReadAsStringAsync(cancellationToken);
                _logger.LogError("Erro ao criar usuário. Status: {Status}, Response: {Response}", response.StatusCode, errorContent);
                
                if (response.StatusCode == System.Net.HttpStatusCode.Conflict)
                {
                    var userId = await GetUserIdByEmailAsync(realmId, email, cancellationToken);
                    if (!string.IsNullOrEmpty(userId))
                    {
                        _logger.LogInformation("Usuário {Email} foi criado por outro processo. Retornando ID {UserId}", email, userId);
                        return userId;
                    }
                }
                
                response.EnsureSuccessStatusCode();
            }

            // Obter ID do usuário criado
            var location = response.Headers.Location?.ToString();
            if (string.IsNullOrEmpty(location))
            {
                var getUserRequest = new HttpRequestMessage(HttpMethod.Get, $"{_keycloakBaseUrl}/admin/realms/{realmId}/users?email={Uri.EscapeDataString(email)}");
                getUserRequest.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", adminToken);
                
                var getUserResponse = await _httpClient.SendAsync(getUserRequest, cancellationToken);
                getUserResponse.EnsureSuccessStatusCode();
                
                var users = await getUserResponse.Content.ReadFromJsonAsync<List<KeycloakUser>>(cancellationToken: cancellationToken);
                var userId = users?.FirstOrDefault()?.Id;

                if (string.IsNullOrEmpty(userId))
                    throw new Exception("Usuário criado mas ID não encontrado");

                _logger.LogInformation("Usuário {Email} criado no realm {RealmId} com ID {UserId}", email, realmId, userId);
                return userId;
            }

            var userIdFromLocation = location.Split('/').Last();
            _logger.LogInformation("Usuário {Email} criado no realm {RealmId} com ID {UserId}", email, realmId, userIdFromLocation);
            return userIdFromLocation;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao criar usuário {Email} no realm {RealmId}", email, realmId);
            throw;
        }
    }


    public async Task AssignRoleAsync(string realmId, string userId, string roleName, CancellationToken cancellationToken = default)
    {
        try
        {
            var adminToken = await GetAdminTokenAsync(cancellationToken);

            var getRoleRequest = new HttpRequestMessage(HttpMethod.Get, $"{_keycloakBaseUrl}/admin/realms/{realmId}/roles/{roleName}");
            getRoleRequest.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", adminToken);

            var getRoleResponse = await _httpClient.SendAsync(getRoleRequest, cancellationToken);
            if (!getRoleResponse.IsSuccessStatusCode)
            {
                await CreateRoleAsync(realmId, roleName, cancellationToken);
                
                var getRoleRequestRetry = new HttpRequestMessage(HttpMethod.Get, $"{_keycloakBaseUrl}/admin/realms/{realmId}/roles/{roleName}");
                getRoleRequestRetry.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", adminToken);
                
                getRoleResponse = await _httpClient.SendAsync(getRoleRequestRetry, cancellationToken);
            }
            getRoleResponse.EnsureSuccessStatusCode();

            var role = await getRoleResponse.Content.ReadFromJsonAsync<KeycloakRole>(cancellationToken: cancellationToken);
            if (role == null)
                throw new Exception($"Role {roleName} não encontrada");

            var assignRequest = new HttpRequestMessage(HttpMethod.Post, $"{_keycloakBaseUrl}/admin/realms/{realmId}/users/{userId}/role-mappings/realm");
            assignRequest.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", adminToken);
            assignRequest.Content = JsonContent.Create(new[] { role });

            var assignResponse = await _httpClient.SendAsync(assignRequest, cancellationToken);
            assignResponse.EnsureSuccessStatusCode();

            _logger.LogInformation("Role {RoleName} atribuída ao usuário {UserId} no realm {RealmId}", roleName, userId, realmId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao atribuir role {RoleName} ao usuário {UserId}", roleName, userId);
            throw;
        }
    }

    public async Task<string> GetAccessTokenAsync(string realmId, string username, string password, CancellationToken cancellationToken = default)
    {
        var result = await GetTokensAsync(realmId, username, password, cancellationToken);
        return result.AccessToken;
    }

    public async Task<KeycloakTokenResult> GetTokensAsync(string realmId, string username, string password, CancellationToken cancellationToken = default)
    {
        try
        {
            var tokenRequest = new Dictionary<string, string>
            {
                { "grant_type", "password" },
                { "client_id", _clientId },
                { "username", username },
                { "password", password }
            };

            var request = new HttpRequestMessage(HttpMethod.Post, $"{_keycloakBaseUrl}/realms/{realmId}/protocol/openid-connect/token")
            {
                Content = new FormUrlEncodedContent(tokenRequest)
            };

            var response = await _httpClient.SendAsync(request, cancellationToken);
            
            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await response.Content.ReadAsStringAsync(cancellationToken);
                _logger.LogError("Erro ao obter token. Status: {Status}, Response: {Response}", response.StatusCode, errorContent);
                throw new HttpRequestException($"Erro ao autenticar no Keycloak: {response.StatusCode}. Response: {errorContent}");
            }

            var tokenResponse = await response.Content.ReadFromJsonAsync<KeycloakTokenResponse>(cancellationToken: cancellationToken);
            if (tokenResponse == null)
                throw new Exception("Token não retornado");

            return new KeycloakTokenResult
            {
                AccessToken = tokenResponse.AccessToken,
                RefreshToken = tokenResponse.RefreshToken,
                ExpiresIn = tokenResponse.ExpiresIn
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao obter token para usuário {Username} no realm {RealmId}", username, realmId);
            throw;
        }
    }

    public async Task<KeycloakTokenResult> RefreshTokenAsync(string realmId, string refreshToken, CancellationToken cancellationToken = default)
    {
        try
        {
            var tokenRequest = new Dictionary<string, string>
            {
                { "grant_type", "refresh_token" },
                { "client_id", _clientId },
                { "refresh_token", refreshToken }
            };

            if (!string.IsNullOrWhiteSpace(_clientSecret))
            {
                tokenRequest["client_secret"] = _clientSecret;
            }

            var tokenBaseUrl = ResolveKeycloakBaseUrlForToken(refreshToken).TrimEnd('/');

            async Task<HttpResponseMessage> SendAsync(string baseUrl)
            {
                var request = new HttpRequestMessage(
                    HttpMethod.Post,
                    $"{baseUrl.TrimEnd('/')}/realms/{realmId}/protocol/openid-connect/token")
                {
                    Content = new FormUrlEncodedContent(tokenRequest)
                };

                return await _httpClient.SendAsync(request, cancellationToken);
            }

            var response = await SendAsync(tokenBaseUrl);
            
            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await response.Content.ReadAsStringAsync(cancellationToken);
                _logger.LogError("Erro ao renovar token. Status: {Status}, Response: {Response}", response.StatusCode, errorContent);

                var internalBaseUrl = _keycloakBaseUrl.TrimEnd('/');
                if (!string.Equals(tokenBaseUrl, internalBaseUrl, StringComparison.OrdinalIgnoreCase))
                {
                    _logger.LogWarning(
                        "Falha ao renovar token usando {TokenBaseUrl}. Tentando fallback para {InternalBaseUrl}.",
                        tokenBaseUrl,
                        internalBaseUrl);

                    response = await SendAsync(internalBaseUrl);
                    if (!response.IsSuccessStatusCode)
                    {
                        var fallbackError = await response.Content.ReadAsStringAsync(cancellationToken);
                        _logger.LogError(
                            "Falha no fallback de renovação de token. Status: {Status}, Response: {Response}",
                            response.StatusCode,
                            fallbackError);
                        
                        // Lançar exceção com informações detalhadas para melhor tratamento no AuthController
                        var errorMessage = $"Refresh token failed with status {response.StatusCode}: {fallbackError}";
                        throw new HttpRequestException(errorMessage);
                    }
                }
                else
                {
                    // Lançar exceção com informações detalhadas para melhor tratamento no AuthController
                    var errorMessage = $"Refresh token failed with status {response.StatusCode}: {errorContent}";
                    throw new HttpRequestException(errorMessage);
                }
            }

            var tokenResponse = await response.Content.ReadFromJsonAsync<KeycloakTokenResponse>(cancellationToken: cancellationToken);
            if (tokenResponse == null)
                throw new Exception("Token não retornado");

            return new KeycloakTokenResult
            {
                AccessToken = tokenResponse.AccessToken,
                RefreshToken = tokenResponse.RefreshToken ?? refreshToken,
                ExpiresIn = tokenResponse.ExpiresIn
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao renovar token no realm {RealmId}", realmId);
            throw;
        }
    }


    public async Task UpdateUserPasswordAsync(string realmId, string userId, string newPassword, CancellationToken cancellationToken = default)
    {
        try
        {
            var adminToken = await GetAdminTokenAsync(cancellationToken);

            var resetRequest = new HttpRequestMessage(HttpMethod.Put, $"{_keycloakBaseUrl}/admin/realms/{realmId}/users/{userId}/reset-password");
            resetRequest.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", adminToken);
            resetRequest.Content = JsonContent.Create(new
            {
                type = "password",
                value = newPassword,
                temporary = false
            });

            var resetResponse = await _httpClient.SendAsync(resetRequest, cancellationToken);
            resetResponse.EnsureSuccessStatusCode();

            _logger.LogInformation("Senha atualizada com sucesso para userId {UserId} no realm {RealmId}", userId, realmId);

            try
            {
                var updateUserRequest = new HttpRequestMessage(HttpMethod.Put, $"{_keycloakBaseUrl}/admin/realms/{realmId}/users/{userId}");
                updateUserRequest.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", adminToken);
                updateUserRequest.Content = JsonContent.Create(new
                {
                    emailVerified = true,
                    enabled = true,
                    requiredActions = Array.Empty<string>()
                });

                var updateUserResponse = await _httpClient.SendAsync(updateUserRequest, cancellationToken);
                if (updateUserResponse.IsSuccessStatusCode)
                {
                    _logger.LogInformation("Usuário {UserId} atualizado: email verificado e required actions removidas", userId);
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Erro ao atualizar usuário {UserId} após reset de senha", userId);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao atualizar senha para userId {UserId} no realm {RealmId}", userId, realmId);
            throw;
        }
    }

    public async Task<string> GetSocialLoginUrlAsync(string realmId, string provider, string redirectUri, CancellationToken cancellationToken = default)
    {
        return await GetSocialLoginUrlAsync(realmId, provider, redirectUri, null, cancellationToken);
    }

    public async Task<string> GetSocialLoginUrlAsync(string realmId, string provider, string redirectUri, string? state, CancellationToken cancellationToken = default)
    {
        try
        {
            var keycloakUrl = ResolveKeycloakBaseUrlForRedirectUri(redirectUri);
            var authUrl = $"{keycloakUrl}/realms/{realmId}/protocol/openid-connect/auth";
            var parameters = new Dictionary<string, string>
            {
                { "client_id", _clientId },
                { "redirect_uri", redirectUri },
                { "response_type", "code" },
                { "scope", "openid profile email" },
                { "kc_idp_hint", provider }
            };

            var themeName = _configuration["Keycloak:ThemeName"];
            if (!string.IsNullOrWhiteSpace(themeName))
            {
                parameters["kc_theme"] = themeName;
            }

            if (!string.IsNullOrEmpty(state))
            {
                parameters.Add("state", state);
            }

            var queryString = string.Join("&", parameters.Select(p => $"{Uri.EscapeDataString(p.Key)}={Uri.EscapeDataString(p.Value)}"));
            return $"{authUrl}?{queryString}";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao gerar URL de login social para provider {Provider} no realm {RealmId}", provider, realmId);
            throw;
        }
    }


    public async Task<KeycloakTokenResult> ExchangeAuthorizationCodeAsync(string realmId, string code, string redirectUri, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Trocando código de autorização por token. Realm: {RealmId}, RedirectUri: {RedirectUri}", realmId, redirectUri);
            
            var tokenBaseUrl = ResolveKeycloakBaseUrlForRedirectUri(redirectUri);
            var tokenRequest = new Dictionary<string, string>
            {
                { "grant_type", "authorization_code" },
                { "client_id", _clientId },
                { "code", code },
                { "redirect_uri", redirectUri }
            };

            var request = new HttpRequestMessage(HttpMethod.Post, $"{tokenBaseUrl}/realms/{realmId}/protocol/openid-connect/token")
            {
                Content = new FormUrlEncodedContent(tokenRequest)
            };

            var response = await _httpClient.SendAsync(request, cancellationToken);
            
            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await response.Content.ReadAsStringAsync(cancellationToken);
                _logger.LogError("Erro ao trocar código por token. Status: {Status}, Response: {Response}", response.StatusCode, errorContent);
                response.EnsureSuccessStatusCode();
            }

            var tokenResponse = await response.Content.ReadFromJsonAsync<KeycloakTokenResponse>(cancellationToken: cancellationToken);
            if (tokenResponse == null)
                throw new Exception("Token não retornado");

            return new KeycloakTokenResult
            {
                AccessToken = tokenResponse.AccessToken,
                RefreshToken = tokenResponse.RefreshToken,
                ExpiresIn = tokenResponse.ExpiresIn
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao trocar código de autorização por token no realm {RealmId}", realmId);
            throw;
        }
    }

    public async Task<KeycloakUserInfo> GetUserInfoAsync(string realmId, string accessToken, CancellationToken cancellationToken = default)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(accessToken))
                throw new ArgumentException("Access token vazio ao chamar userinfo", nameof(accessToken));

            var userInfoBaseUrl = ResolveKeycloakBaseUrlForToken(accessToken);
            var request = new HttpRequestMessage(HttpMethod.Get, $"{userInfoBaseUrl}/realms/{realmId}/protocol/openid-connect/userinfo");
            request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", accessToken);

            var response = await _httpClient.SendAsync(request, cancellationToken);
            response.EnsureSuccessStatusCode();

            var userInfo = await response.Content.ReadFromJsonAsync<KeycloakUserInfo>(cancellationToken: cancellationToken);
            if (userInfo == null)
                throw new Exception("UserInfo não retornado");

            if (string.IsNullOrWhiteSpace(userInfo.Email) && !string.IsNullOrWhiteSpace(userInfo.Sub))
            {
                _logger.LogWarning("UserInfo retornado sem email. Tentando buscar diretamente do usuário no Keycloak. Sub={Sub}", userInfo.Sub);
                
                try
                {
                    var userDetails = await GetUserByIdAsync(realmId, userInfo.Sub, cancellationToken);
                    if (userDetails != null && !string.IsNullOrWhiteSpace(userDetails.Email))
                    {
                        userInfo.Email = userDetails.Email;
                        
                        if (string.IsNullOrWhiteSpace(userInfo.GivenName) && !string.IsNullOrWhiteSpace(userDetails.FirstName))
                            userInfo.GivenName = userDetails.FirstName;
                        if (string.IsNullOrWhiteSpace(userInfo.FamilyName) && !string.IsNullOrWhiteSpace(userDetails.LastName))
                            userInfo.FamilyName = userDetails.LastName;
                        if (string.IsNullOrWhiteSpace(userInfo.Name) && !string.IsNullOrWhiteSpace(userDetails.Username))
                            userInfo.Name = userDetails.Username;
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Erro ao buscar usuário diretamente no Keycloak. Sub={Sub}", userInfo.Sub);
                }
            }

            if (string.IsNullOrWhiteSpace(userInfo.Email))
            {
                _logger.LogError("UserInfo retornado sem email mesmo após buscar diretamente. Sub={Sub}", userInfo.Sub);
                throw new Exception("Email não foi retornado pelo Keycloak");
            }

            return userInfo;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao obter informações do usuário no realm {RealmId}", realmId);
            throw;
        }
    }

    private string ResolveKeycloakBaseUrlForRedirectUri(string redirectUri)
    {
        if (Uri.TryCreate(redirectUri, UriKind.Absolute, out var uri))
        {
            if (string.Equals(uri.Host, "localhost", StringComparison.OrdinalIgnoreCase)
                || string.Equals(uri.Host, "127.0.0.1", StringComparison.OrdinalIgnoreCase)
                || string.Equals(uri.Host, "::1", StringComparison.OrdinalIgnoreCase))
            {
                return _keycloakBaseUrl.TrimEnd('/');
            }
        }

        return _keycloakBaseUrl.TrimEnd('/');
    }

    private string ResolveKeycloakBaseUrlForToken(string token)
    {
        if (TryGetIssuerBaseUrlFromJwt(token, out var issuerBaseUrl))
            return issuerBaseUrl;

        return _keycloakBaseUrl.TrimEnd('/');
    }

    private static bool TryGetIssuerBaseUrlFromJwt(string jwt, out string issuerBaseUrl)
    {
        issuerBaseUrl = string.Empty;
        var payload = TryGetJwtPayload(jwt);
        if (payload == null)
            return false;

        if (!payload.Value.TryGetProperty("iss", out var issProp))
            return false;

        var iss = issProp.GetString();
        if (string.IsNullOrWhiteSpace(iss))
            return false;

        var marker = "/realms/";
        var idx = iss.IndexOf(marker, StringComparison.OrdinalIgnoreCase);
        if (idx <= 0)
            return false;

        issuerBaseUrl = iss[..idx].TrimEnd('/');
        return true;
    }


    private static JsonElement? TryGetJwtPayload(string jwt)
    {
        try
        {
            var parts = jwt.Split('.');
            if (parts.Length < 2)
                return null;

            var payloadBytes = Base64UrlDecode(parts[1]);
            using var doc = JsonDocument.Parse(payloadBytes);
            return doc.RootElement.Clone();
        }
        catch
        {
            return null;
        }
    }

    private static byte[] Base64UrlDecode(string input)
    {
        var padded = input.Replace('-', '+').Replace('_', '/');
        switch (padded.Length % 4)
        {
            case 2: padded += "=="; break;
            case 3: padded += "="; break;
        }
        return Convert.FromBase64String(padded);
    }

    public async Task LogoutAsync(string realmId, string refreshToken, CancellationToken cancellationToken = default)
    {
        try
        {
            var logoutRequest = new Dictionary<string, string>
            {
                { "client_id", _clientId },
                { "refresh_token", refreshToken }
            };

            if (!string.IsNullOrWhiteSpace(_clientSecret))
            {
                logoutRequest["client_secret"] = _clientSecret;
            }

            var logoutBaseUrl = ResolveKeycloakBaseUrlForToken(refreshToken).TrimEnd('/');

            async Task<HttpResponseMessage> SendAsync(string baseUrl)
            {
                var request = new HttpRequestMessage(
                    HttpMethod.Post,
                    $"{baseUrl.TrimEnd('/')}/realms/{realmId}/protocol/openid-connect/logout")
                {
                    Content = new FormUrlEncodedContent(logoutRequest)
                };

                return await _httpClient.SendAsync(request, cancellationToken);
            }

            var response = await SendAsync(logoutBaseUrl);
            if (!response.IsSuccessStatusCode)
            {
                var internalBaseUrl = _keycloakBaseUrl.TrimEnd('/');
                if (!string.Equals(logoutBaseUrl, internalBaseUrl, StringComparison.OrdinalIgnoreCase))
                {
                    _logger.LogWarning(
                        "Falha ao invalidar sessão usando {LogoutBaseUrl}. Tentando fallback para {InternalBaseUrl}.",
                        logoutBaseUrl,
                        internalBaseUrl);
                    response = await SendAsync(internalBaseUrl);
                }
            }
            
            if (response.StatusCode == System.Net.HttpStatusCode.BadRequest)
            {
                _logger.LogWarning("Refresh token já estava invalidado no realm {RealmId}", realmId);
                return;
            }

            response.EnsureSuccessStatusCode();
            _logger.LogInformation("Sessão invalidada com sucesso no realm {RealmId}", realmId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao invalidar sessão no realm {RealmId}", realmId);
        }
    }

    public async Task ConfigureGoogleIdentityProviderAsync(string realmId, string clientId, string clientSecret, CancellationToken cancellationToken = default)
    {
        try
        {
            var adminToken = await GetAdminTokenAsync(cancellationToken);

            var checkRequest = new HttpRequestMessage(HttpMethod.Get, $"{_keycloakBaseUrl}/admin/realms/{realmId}/identity-provider/instances");
            checkRequest.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", adminToken);
            
            var checkResponse = await _httpClient.SendAsync(checkRequest, cancellationToken);
            if (checkResponse.IsSuccessStatusCode)
            {
                var existingProviders = await checkResponse.Content.ReadFromJsonAsync<List<Dictionary<string, object>>>(cancellationToken: cancellationToken);
                var googleProvider = existingProviders?.FirstOrDefault(p => 
                    p.ContainsKey("alias") && p["alias"]?.ToString() == "google");
                
                if (googleProvider != null)
                {
                    // Sempre atualizar o provider para garantir que o redirect URI seja recalculado
                    // baseado no frontendUrl atualizado do realm
                    var updateRequest = new
                    {
                        alias = "google",
                        providerId = "google",
                        enabled = true,
                        trustEmail = true,
                        storeToken = false,
                        addReadTokenRoleOnCreate = false,
                        firstBrokerLoginFlowAlias = "first broker login",
                        config = new Dictionary<string, string>
                        {
                            { "clientId", clientId },
                            { "clientSecret", clientSecret },
                            { "defaultScope", "openid profile email" }
                        }
                    };

                    var updateHttpRequest = new HttpRequestMessage(HttpMethod.Put, $"{_keycloakBaseUrl}/admin/realms/{realmId}/identity-provider/instances/google");
                    updateHttpRequest.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", adminToken);
                    updateHttpRequest.Content = JsonContent.Create(updateRequest);

                    var updateResponse = await _httpClient.SendAsync(updateHttpRequest, cancellationToken);
                    if (updateResponse.IsSuccessStatusCode)
                    {
                        _logger.LogInformation("Google Identity Provider atualizado com sucesso no realm {RealmId}", realmId);
                        await ConfigureGoogleIdentityProviderMappersAsync(realmId, adminToken, cancellationToken);
                        LogRedirectUriInfo(realmId);
                        return;
                    }
                    else
                    {
                        var errorContent = await updateResponse.Content.ReadAsStringAsync(cancellationToken);
                        _logger.LogWarning("Erro ao atualizar Google Identity Provider. Status: {Status}, Response: {Response}", 
                            updateResponse.StatusCode, errorContent);
                    }
                }
            }

            // Criar novo provider
            var idpRequest = new
            {
                alias = "google",
                providerId = "google",
                enabled = true,
                trustEmail = true,
                storeToken = false,
                addReadTokenRoleOnCreate = false,
                firstBrokerLoginFlowAlias = "first broker login",
                config = new Dictionary<string, string>
                {
                    { "clientId", clientId },
                    { "clientSecret", clientSecret },
                    { "defaultScope", "openid profile email" }
                }
            };

            var request = new HttpRequestMessage(HttpMethod.Post, $"{_keycloakBaseUrl}/admin/realms/{realmId}/identity-provider/instances");
            request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", adminToken);
            request.Content = JsonContent.Create(idpRequest);

            var response = await _httpClient.SendAsync(request, cancellationToken);
            
            if (response.StatusCode == System.Net.HttpStatusCode.Conflict)
            {
                _logger.LogInformation("Google Identity Provider já existe no realm {RealmId}. Configurando mappers...", realmId);
                await ConfigureGoogleIdentityProviderMappersAsync(realmId, adminToken, cancellationToken);
                LogRedirectUriInfo(realmId);
                return;
            }
            
            response.EnsureSuccessStatusCode();

            _logger.LogInformation("Google Identity Provider configurado com sucesso no realm {RealmId}", realmId);
            
            await ConfigureGoogleIdentityProviderMappersAsync(realmId, adminToken, cancellationToken);
            
            LogRedirectUriInfo(realmId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao configurar Google Identity Provider no realm {RealmId}", realmId);
            throw;
        }
    }

    private async Task ConfigureGoogleIdentityProviderMappersAsync(string realmId, string adminToken, CancellationToken cancellationToken)
    {
        try
        {
            var mappers = new[]
            {
                new
                {
                    name = "email",
                    identityProviderAlias = "google",
                    identityProviderMapper = "oidc-user-attribute-idp-mapper",
                    config = new Dictionary<string, string>
                    {
                        { "claim", "email" },
                        { "user.attribute", "email" },
                        { "syncMode", "INHERIT" }
                    }
                },
                new
                {
                    name = "given_name",
                    identityProviderAlias = "google",
                    identityProviderMapper = "oidc-user-attribute-idp-mapper",
                    config = new Dictionary<string, string>
                    {
                        { "claim", "given_name" },
                        { "user.attribute", "firstName" },
                        { "syncMode", "INHERIT" }
                    }
                },
                new
                {
                    name = "family_name",
                    identityProviderAlias = "google",
                    identityProviderMapper = "oidc-user-attribute-idp-mapper",
                    config = new Dictionary<string, string>
                    {
                        { "claim", "family_name" },
                        { "user.attribute", "lastName" },
                        { "syncMode", "INHERIT" }
                    }
                },
                new
                {
                    name = "name",
                    identityProviderAlias = "google",
                    identityProviderMapper = "oidc-user-attribute-idp-mapper",
                    config = new Dictionary<string, string>
                    {
                        { "claim", "name" },
                        { "user.attribute", "name" },
                        { "syncMode", "INHERIT" }
                    }
                }
            };

            var getMappersRequest = new HttpRequestMessage(HttpMethod.Get, $"{_keycloakBaseUrl}/admin/realms/{realmId}/identity-provider/instances/google/mappers");
            getMappersRequest.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", adminToken);
            
            var getMappersResponse = await _httpClient.SendAsync(getMappersRequest, cancellationToken);
            var existingMappers = new List<Dictionary<string, object>>();
            
            if (getMappersResponse.IsSuccessStatusCode)
            {
                existingMappers = await getMappersResponse.Content.ReadFromJsonAsync<List<Dictionary<string, object>>>(cancellationToken: cancellationToken) 
                    ?? new List<Dictionary<string, object>>();
            }

            foreach (var mapper in mappers)
            {
                var existingMapper = existingMappers.FirstOrDefault(m => 
                    m.ContainsKey("name") && m["name"]?.ToString() == mapper.name);

                if (existingMapper != null)
                {
                    var updateRequest = new HttpRequestMessage(HttpMethod.Put, 
                        $"{_keycloakBaseUrl}/admin/realms/{realmId}/identity-provider/instances/google/mappers/{existingMapper["id"]?.ToString()}");
                    updateRequest.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", adminToken);
                    updateRequest.Content = JsonContent.Create(mapper);

                    var updateResponse = await _httpClient.SendAsync(updateRequest, cancellationToken);
                    if (updateResponse.IsSuccessStatusCode)
                    {
                        _logger.LogInformation("Mapper '{MapperName}' atualizado no Google Identity Provider", mapper.name);
                    }
                }
                else
                {
                    var createRequest = new HttpRequestMessage(HttpMethod.Post, 
                        $"{_keycloakBaseUrl}/admin/realms/{realmId}/identity-provider/instances/google/mappers");
                    createRequest.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", adminToken);
                    createRequest.Content = JsonContent.Create(mapper);

                    var createResponse = await _httpClient.SendAsync(createRequest, cancellationToken);
                    if (createResponse.IsSuccessStatusCode)
                    {
                        _logger.LogInformation("Mapper '{MapperName}' criado no Google Identity Provider", mapper.name);
                    }
                }
            }

            _logger.LogInformation("Mappers do Google Identity Provider configurados no realm {RealmId}", realmId);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Erro ao configurar mappers do Google Identity Provider no realm {RealmId}", realmId);
        }
    }

    private void LogRedirectUriInfo(string realmId)
    {
        // O redirect URI do Google Identity Provider é calculado pelo Keycloak baseado no frontendUrl do realm
        var baseUrl = _keycloakBaseUrl.TrimEnd('/');
        
        // Garantir HTTPS
        if (baseUrl.StartsWith("http://", StringComparison.OrdinalIgnoreCase))
        {
            baseUrl = baseUrl.Replace("http://", "https://", StringComparison.OrdinalIgnoreCase);
        }
        
        var googleRedirectUri = $"{baseUrl}/realms/{realmId}/broker/google/endpoint";
        
        _logger.LogWarning(
            "IMPORTANTE: Adicione este Redirect URI no Google Cloud Console:\n" +
            "  {RedirectUri}\n" +
            "  Acesse: https://console.cloud.google.com/apis/credentials\n" +
            "  Edite seu OAuth 2.0 Client ID e adicione o URI acima em 'Authorized redirect URIs'\n" +
            "  (O Keycloak calcula este URI automaticamente baseado no frontendUrl do realm)",
            googleRedirectUri);
    }


    public async Task EnsureClientExistsAsync(string realmId, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Verificando se client {ClientId} existe no realm {RealmId}", _clientId, realmId);
            await CreateClientAsync(realmId, cancellationToken);
            _logger.LogInformation("Client {ClientId} garantido no realm {RealmId}", _clientId, realmId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao garantir que client {ClientId} existe no realm {RealmId}", _clientId, realmId);
            throw;
        }
    }

    public async Task ConfigureRealmThemeAsync(string realmId, string themeName, CancellationToken cancellationToken = default)
    {
        try
        {
            var adminToken = await GetAdminTokenAsync(cancellationToken);
            
            // Obter configuração atual do realm
            var getRequest = new HttpRequestMessage(HttpMethod.Get, $"{_keycloakBaseUrl}/admin/realms/{realmId}");
            getRequest.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", adminToken);
            
            var getResponse = await _httpClient.SendAsync(getRequest, cancellationToken);
            if (!getResponse.IsSuccessStatusCode)
            {
                _logger.LogWarning("Não foi possível obter configuração do realm {RealmId} para configurar theme", realmId);
                return;
            }
            
            // Ler o JSON como string primeiro para poder reutilizar
            var realmJson = await getResponse.Content.ReadAsStringAsync(cancellationToken);
            var realmConfig = JsonSerializer.Deserialize<JsonElement>(realmJson);
            
            // Verificar se o theme já está configurado
            var currentLoginTheme = realmConfig.TryGetProperty("loginTheme", out var loginThemeProp) 
                ? loginThemeProp.GetString() 
                : null;
            
            if (currentLoginTheme == themeName)
            {
                _logger.LogInformation("Realm {RealmId} já possui theme {ThemeName} configurado", realmId, themeName);
                return;
            }
            
            // Deserializar para dicionário para poder modificar
            var jsonOptions = new JsonSerializerOptions { WriteIndented = false };
            var realmDict = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(realmJson, jsonOptions) 
                ?? new Dictionary<string, JsonElement>();
            
            // Criar payload de atualização com todas as propriedades do realm
            var updatePayload = new Dictionary<string, object>();
            foreach (var kvp in realmDict)
            {
                if (kvp.Key == "id")
                {
                    continue; // Não incluir id no update
                }
                
                // Converter JsonElement para object
                object? value = kvp.Value.ValueKind switch
                {
                    JsonValueKind.String => kvp.Value.GetString() ?? string.Empty,
                    JsonValueKind.Number => kvp.Value.TryGetInt32(out var intVal) ? intVal : (object)kvp.Value.GetDouble(),
                    JsonValueKind.True => true,
                    JsonValueKind.False => false,
                    JsonValueKind.Null => null,
                    _ => kvp.Value
                };
                
                if (value != null)
                {
                    updatePayload[kvp.Key] = value;
                }
            }
            
            // Atualizar themes
            updatePayload["loginTheme"] = themeName;
            updatePayload["accountTheme"] = themeName;
            updatePayload["adminTheme"] = "keycloak"; // Manter theme padrão para admin
            updatePayload["emailTheme"] = themeName;
            
            var updateRequest = new HttpRequestMessage(HttpMethod.Put, $"{_keycloakBaseUrl}/admin/realms/{realmId}");
            updateRequest.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", adminToken);
            updateRequest.Content = JsonContent.Create(updatePayload);
            
            var updateResponse = await _httpClient.SendAsync(updateRequest, cancellationToken);
            if (updateResponse.IsSuccessStatusCode)
            {
                _logger.LogInformation("Theme {ThemeName} configurado com sucesso no realm {RealmId}", themeName, realmId);
            }
            else
            {
                var errorContent = await updateResponse.Content.ReadAsStringAsync(cancellationToken);
                _logger.LogWarning("Erro ao configurar theme {ThemeName} no realm {RealmId}. Status: {Status}, Response: {Response}", 
                    themeName, realmId, updateResponse.StatusCode, errorContent);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao configurar theme {ThemeName} no realm {RealmId}", themeName, realmId);
            throw;
        }
    }

    public async Task ConfigureRealmProvidersAsync(string realmId, CancellationToken cancellationToken = default)
    {
        try
        {
            var googleClientId = _configuration["Keycloak:Google:ClientId"];
            var googleClientSecret = _configuration["Keycloak:Google:ClientSecret"];
            
            if (!string.IsNullOrWhiteSpace(googleClientId) && !string.IsNullOrWhiteSpace(googleClientSecret))
            {
                await ConfigureGoogleIdentityProviderAsync(realmId, googleClientId, googleClientSecret, cancellationToken);
                _logger.LogInformation("Google Identity Provider configurado/atualizado no realm {RealmId}", realmId);
            }
            else
            {
                _logger.LogWarning("Credenciais do Google não encontradas em appsettings. Google Identity Provider não será configurado no realm {RealmId}", realmId);
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Erro ao configurar Google Identity Provider no realm {RealmId}", realmId);
        }
    }

    public async Task<string?> GetUserIdByEmailAsync(string realmId, string email, CancellationToken cancellationToken = default)
    {
        try
        {
            var adminToken = await GetAdminTokenAsync(cancellationToken);
            
            var getUserRequest = new HttpRequestMessage(HttpMethod.Get, $"{_keycloakBaseUrl}/admin/realms/{realmId}/users?email={Uri.EscapeDataString(email)}");
            getUserRequest.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", adminToken);
            
            var getUserResponse = await _httpClient.SendAsync(getUserRequest, cancellationToken);
            if (!getUserResponse.IsSuccessStatusCode)
            {
                return null;
            }
            
            var users = await getUserResponse.Content.ReadFromJsonAsync<List<KeycloakUser>>(cancellationToken: cancellationToken);
            return users?.FirstOrDefault()?.Id;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Erro ao buscar usuário {Email} no realm {RealmId}", email, realmId);
            return null;
        }
    }

    public async Task DeleteUserAsync(string realmId, string userId, CancellationToken cancellationToken = default)
    {
        try
        {
            var adminToken = await GetAdminTokenAsync(cancellationToken);
            
            var deleteRequest = new HttpRequestMessage(HttpMethod.Delete, $"{_keycloakBaseUrl}/admin/realms/{realmId}/users/{userId}");
            deleteRequest.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", adminToken);
            
            var deleteResponse = await _httpClient.SendAsync(deleteRequest, cancellationToken);
            
            if (!deleteResponse.IsSuccessStatusCode)
            {
                var errorContent = await deleteResponse.Content.ReadAsStringAsync(cancellationToken);
                _logger.LogWarning("Erro ao deletar usuário {UserId} do realm {RealmId}. Status: {Status}, Response: {Response}", 
                    userId, realmId, deleteResponse.StatusCode, errorContent);
            }
            else
            {
                _logger.LogInformation("Usuário {UserId} deletado com sucesso do realm {RealmId}", userId, realmId);
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Erro ao deletar usuário {UserId} do realm {RealmId}", userId, realmId);
        }
    }

    private async Task<string> GetAdminTokenAsync(CancellationToken cancellationToken)
    {
        if (_memoryCache.TryGetValue(AdminTokenCacheKey, out string? cachedToken) && !string.IsNullOrWhiteSpace(cachedToken))
        {
            _logger.LogDebug("Token de admin encontrado no cache");
            return cachedToken;
        }

        await _tokenSemaphore.WaitAsync(cancellationToken);
        try
        {
            if (_memoryCache.TryGetValue(AdminTokenCacheKey, out cachedToken) && !string.IsNullOrWhiteSpace(cachedToken))
            {
                _logger.LogDebug("Token de admin encontrado no cache após lock");
                return cachedToken;
            }

            var tokenResponse = await RequestAdminTokenAsync(cancellationToken);
            
            var expiresIn = tokenResponse.ExpiresIn > 0 ? tokenResponse.ExpiresIn : 300;
            var expirationTime = TimeSpan.FromSeconds(expiresIn * 0.9);
            
            var cacheOptions = new MemoryCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = expirationTime,
                SlidingExpiration = null
            };
            
            _memoryCache.Set(AdminTokenCacheKey, tokenResponse.AccessToken, cacheOptions);
            
            _logger.LogInformation("Token de admin obtido e armazenado no cache. Expira em {ExpirationTime} segundos", expiresIn);
            
            return tokenResponse.AccessToken;
        }
        finally
        {
            _tokenSemaphore.Release();
        }
    }

    private async Task<KeycloakTokenResponse> RequestAdminTokenAsync(CancellationToken cancellationToken)
    {
        if (!string.IsNullOrWhiteSpace(_adminClientSecret))
        {
            try
            {
                _logger.LogDebug("Tentando autenticação via client_credentials com client {ClientId}", _adminClientId);
                
                var tokenRequest = new Dictionary<string, string>
                {
                    { "grant_type", "client_credentials" },
                    { "client_id", _adminClientId },
                    { "client_secret", _adminClientSecret }
                };

                var request = new HttpRequestMessage(HttpMethod.Post, $"{_keycloakBaseUrl}/realms/{_adminRealm}/protocol/openid-connect/token")
                {
                    Content = new FormUrlEncodedContent(tokenRequest)
                };

                var response = await _httpClient.SendAsync(request, cancellationToken);
                if (response.IsSuccessStatusCode)
                {
                    var clientCredentialsResponse = await response.Content.ReadAsStringAsync(cancellationToken);
                    var tokenResponse = JsonSerializer.Deserialize<KeycloakTokenResponse>(clientCredentialsResponse);
                    
                    if (tokenResponse == null || string.IsNullOrWhiteSpace(tokenResponse.AccessToken))
                    {
                        _logger.LogError("Token de admin vazio na resposta. Response completo: {Response}", clientCredentialsResponse);
                        throw new Exception($"Token de admin não retornado. Response: {clientCredentialsResponse}");
                    }
                    
                    _logger.LogInformation("Token de admin obtido com sucesso via client_credentials");
                    return tokenResponse;
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Falha ao obter token via client_credentials, tentando password grant");
            }
        }

        _logger.LogDebug("Autenticando via password grant com usuário {Username}", _adminUsername);
        
        var passwordTokenRequest = new Dictionary<string, string>
        {
            { "grant_type", "password" },
            { "client_id", _adminClientId },
            { "username", _adminUsername },
            { "password", _adminPassword }
        };

        var passwordRequest = new HttpRequestMessage(HttpMethod.Post, $"{_keycloakBaseUrl}/realms/{_adminRealm}/protocol/openid-connect/token")
        {
            Content = new FormUrlEncodedContent(passwordTokenRequest)
        };

        var passwordResponse = await _httpClient.SendAsync(passwordRequest, cancellationToken);
        
        if (!passwordResponse.IsSuccessStatusCode)
        {
            var errorContent = await passwordResponse.Content.ReadAsStringAsync(cancellationToken);
            _logger.LogError("Erro ao obter token de admin. URL: {Url}, Status: {Status}, Response: {Response}", 
                passwordRequest.RequestUri, passwordResponse.StatusCode, errorContent);
            
            if (passwordResponse.StatusCode == System.Net.HttpStatusCode.NotFound)
            {
                throw new HttpRequestException(
                    $"Keycloak não encontrado em {_keycloakBaseUrl}. " +
                    $"Verifique se o Keycloak está rodando e se a URL está correta. " +
                    $"Tente acessar: {_keycloakBaseUrl}/realms/master no navegador."
                );
            }
            
            throw new HttpRequestException(
                $"Erro ao autenticar no Keycloak: {passwordResponse.StatusCode}. " +
                $"URL: {passwordRequest.RequestUri}. " +
                $"Response: {errorContent}. " +
                $"Verifique as credenciais do admin em appsettings.json"
            );
        }

        var passwordResponseContent = await passwordResponse.Content.ReadAsStringAsync(cancellationToken);
        var passwordTokenResponse = JsonSerializer.Deserialize<KeycloakTokenResponse>(passwordResponseContent);
        
        if (passwordTokenResponse == null || string.IsNullOrWhiteSpace(passwordTokenResponse.AccessToken))
        {
            _logger.LogError("Token de admin vazio na resposta. Response completo: {Response}", passwordResponseContent);
            throw new Exception($"Token de admin não retornado. Response: {passwordResponseContent}");
        }
        
        _logger.LogInformation("Token de admin obtido com sucesso via password grant");
        return passwordTokenResponse;
    }

    private async Task CreateClientAsync(string realmId, CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogInformation("Iniciando criação do client {ClientId} no realm {RealmId}", _clientId, realmId);
            
            var adminToken = await GetAdminTokenAsync(cancellationToken);

            var getClientRequest = new HttpRequestMessage(HttpMethod.Get, $"{_keycloakBaseUrl}/admin/realms/{realmId}/clients?clientId={_clientId}");
            getClientRequest.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", adminToken);

            var getClientResponse = await _httpClient.SendAsync(getClientRequest, cancellationToken);
            if (getClientResponse.IsSuccessStatusCode)
            {
                var existingClients = await getClientResponse.Content.ReadFromJsonAsync<List<KeycloakClient>>(cancellationToken: cancellationToken);
                var existingClient = existingClients?.FirstOrDefault(c => c.ClientId == _clientId);
                if (existingClient != null)
                {
                    _logger.LogInformation("Client {ClientId} já existe no realm {RealmId}. Atualizando configurações...", _clientId, realmId);
                    await UpdateClientAsync(realmId, existingClient.Id, cancellationToken);
                    return;
                }
            }

            _logger.LogInformation("Criando client {ClientId} como público (sem secret) no realm {RealmId}", _clientId, realmId);
            
            var redirectUris = BuildRedirectUris();
            var webOrigins = BuildWebOrigins();
            _logger.LogInformation("Config client redirectUris: {RedirectUris}", string.Join(", ", redirectUris));

            var clientRequest = new
            {
                clientId = _clientId,
                enabled = true,
                standardFlowEnabled = true,
                directAccessGrantsEnabled = true,
                serviceAccountsEnabled = false,
                publicClient = true,
                protocol = "openid-connect",
                redirectUris,
                webOrigins,
                attributes = new Dictionary<string, string>
                {
                    { "access.token.lifespan", "3600" },
                    { "access.token.lifespan.implicit", "3600" },
                    { "sso.session.idle.timeout", "1800" },
                    { "sso.session.max.lifespan", "36000" }
                }
            };

            var createRequest = new HttpRequestMessage(HttpMethod.Post, $"{_keycloakBaseUrl}/admin/realms/{realmId}/clients");
            createRequest.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", adminToken);
            createRequest.Content = JsonContent.Create(clientRequest);

            var createResponse = await _httpClient.SendAsync(createRequest, cancellationToken);
            
            if (!createResponse.IsSuccessStatusCode)
            {
                var errorContent = await createResponse.Content.ReadAsStringAsync(cancellationToken);
                _logger.LogError("Erro ao criar client. Status: {Status}, Response: {Response}", createResponse.StatusCode, errorContent);
                throw new HttpRequestException($"Erro ao criar client {_clientId} no realm {realmId}. Status: {createResponse.StatusCode}. Response: {errorContent}");
            }

            _logger.LogInformation("Client {ClientId} criado com sucesso no realm {RealmId} como público (sem secret)", _clientId, realmId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao criar client {ClientId} no realm {RealmId}", _clientId, realmId);
            throw;
        }
    }

    private async Task UpdateClientAsync(string realmId, string clientUuid, CancellationToken cancellationToken)
    {
        try
        {
            var adminToken = await GetAdminTokenAsync(cancellationToken);

            var redirectUris = BuildRedirectUris();
            var webOrigins = BuildWebOrigins();
            _logger.LogInformation("Atualizando client redirectUris: {RedirectUris}", string.Join(", ", redirectUris));

            var clientUpdate = new
            {
                enabled = true,
                standardFlowEnabled = true,
                directAccessGrantsEnabled = true,
                serviceAccountsEnabled = false,
                publicClient = true,
                redirectUris,
                webOrigins,
                attributes = new Dictionary<string, string>
                {
                    { "access.token.lifespan", "3600" },
                    { "access.token.lifespan.implicit", "3600" },
                    { "sso.session.idle.timeout", "1800" },
                    { "sso.session.max.lifespan", "36000" }
                }
            };

            var updateRequest = new HttpRequestMessage(HttpMethod.Put, $"{_keycloakBaseUrl}/admin/realms/{realmId}/clients/{clientUuid}");
            updateRequest.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", adminToken);
            updateRequest.Content = JsonContent.Create(clientUpdate);

            var updateResponse = await _httpClient.SendAsync(updateRequest, cancellationToken);
            
            if (!updateResponse.IsSuccessStatusCode)
            {
                var errorContent = await updateResponse.Content.ReadAsStringAsync(cancellationToken);
                _logger.LogWarning("Erro ao atualizar client. Status: {Status}, Response: {Response}", updateResponse.StatusCode, errorContent);
            }
            else
            {
                _logger.LogInformation("Client {ClientId} atualizado com sucesso no realm {RealmId}", _clientId, realmId);
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Erro ao atualizar client {ClientId} no realm {RealmId}", _clientId, realmId);
        }
    }

    private string[] BuildRedirectUris()
    {
        var set = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        // Prioridade 1: RedirectUris explícitas (configuração principal)
        var redirectUris = _configuration.GetSection("Keycloak:RedirectUris").Get<string[]>();
        if (redirectUris is { Length: > 0 })
        {
            foreach (var uri in redirectUris)
            {
                var trimmed = (uri ?? string.Empty).Trim();
                if (!string.IsNullOrWhiteSpace(trimmed))
                    set.Add(trimmed);
            }
            _logger.LogInformation("Usando RedirectUris explícitas do appsettings: {Count} URIs", set.Count);
        }
        else
        {
            // Fallback: Se RedirectUris não estiver configurado, usar lógica antiga (compatibilidade)
            _logger.LogWarning("Keycloak:RedirectUris não configurado. Usando lógica automática (fallback).");
            
            // BFF callback targets (backend)
            var apiBaseUrl = _configuration["Api:BaseUrl"];
            if (!string.IsNullOrWhiteSpace(apiBaseUrl))
            {
                set.Add($"{apiBaseUrl.TrimEnd('/')}/auth/oauth-callback");
            }

            // Web app (optional)
            var frontendBaseUrl = (_configuration["Frontend:BaseUrl"] ?? "").TrimEnd('/');
            if (!string.IsNullOrWhiteSpace(frontendBaseUrl))
                set.Add($"{frontendBaseUrl}/*");

            // Mobile (AppAuth)
            set.Add(MobileRedirectUri1);
            set.Add(MobileRedirectUri2);
            set.Add(MobileRedirectUri3);
        }

        // Prioridade 2: ExtraRedirectUris (adicionais, úteis para URLs temporárias, tunnels, etc.)
        var extraRedirectUris = _configuration.GetSection("Keycloak:ExtraRedirectUris").Get<string[]>();
        if (extraRedirectUris is { Length: > 0 })
        {
            foreach (var uri in extraRedirectUris)
            {
                var trimmed = (uri ?? string.Empty).Trim();
                if (!string.IsNullOrWhiteSpace(trimmed))
                    set.Add(trimmed);
            }
        }

        return set.ToArray();
    }

    private string[] BuildWebOrigins()
    {
        var set = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        var frontendBaseUrl = (_configuration["Frontend:BaseUrl"] ?? "").TrimEnd('/');
        if (!string.IsNullOrWhiteSpace(frontendBaseUrl))
            set.Add(frontendBaseUrl);

        // Mobile does not need web origins, but keeping wildcard in DEV helps.
        set.Add("*");

        return set.ToArray();
    }

    private async Task CreateRoleAsync(string realmId, string roleName, CancellationToken cancellationToken)
    {
        var adminToken = await GetAdminTokenAsync(cancellationToken);

        var roleRequest = new
        {
            name = roleName
        };

        var request = new HttpRequestMessage(HttpMethod.Post, $"{_keycloakBaseUrl}/admin/realms/{realmId}/roles");
        request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", adminToken);
        request.Content = JsonContent.Create(roleRequest);

        var response = await _httpClient.SendAsync(request, cancellationToken);
        response.EnsureSuccessStatusCode();
    }

    private async Task<KeycloakUser?> GetUserByIdAsync(string realmId, string userId, CancellationToken cancellationToken = default)
    {
        try
        {
            var adminToken = await GetAdminTokenAsync(cancellationToken);
            
            var getUserRequest = new HttpRequestMessage(HttpMethod.Get, $"{_keycloakBaseUrl}/admin/realms/{realmId}/users/{userId}");
            getUserRequest.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", adminToken);
            
            var getUserResponse = await _httpClient.SendAsync(getUserRequest, cancellationToken);
            if (!getUserResponse.IsSuccessStatusCode)
            {
                _logger.LogWarning("Usuário {UserId} não encontrado no realm {RealmId}. Status: {Status}", userId, realmId, getUserResponse.StatusCode);
                return null;
            }
            
            var user = await getUserResponse.Content.ReadFromJsonAsync<KeycloakUser>(cancellationToken: cancellationToken);
            return user;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Erro ao buscar usuário {UserId} no realm {RealmId}", userId, realmId);
            return null;
        }
    }

    private class KeycloakUser
    {
        public string Id { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public bool? EmailVerified { get; set; }
        public bool Enabled { get; set; } = true;
        public string? FirstName { get; set; }
        public string? LastName { get; set; }
        public string? Username { get; set; }
    }

    private class KeycloakRole
    {
        public string Id { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
    }

    private class KeycloakTokenResponse
    {
        [JsonPropertyName("access_token")]
        public string AccessToken { get; set; } = string.Empty;
        
        [JsonPropertyName("expires_in")]
        public int ExpiresIn { get; set; }
        
        [JsonPropertyName("token_type")]
        public string TokenType { get; set; } = string.Empty;
        
        [JsonPropertyName("refresh_token")]
        public string? RefreshToken { get; set; }
    }

    private class KeycloakClient
    {
        public string Id { get; set; } = string.Empty;
        public string ClientId { get; set; } = string.Empty;
    }
}
