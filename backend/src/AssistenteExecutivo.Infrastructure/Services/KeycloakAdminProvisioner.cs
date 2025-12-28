using System.Net.Http.Json;
using System.Text.Json;
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

            // Tenta importar do JSON primeiro (se configurado)
            var realmJsonPath = _configuration["Keycloak:RealmJsonPath"];
            var useJsonImport = _configuration.GetValue<bool>("Keycloak:UseJsonImport", false);

            // Resolve caminho do JSON (caminho direto - arquivo está dentro do projeto C#)
            string? resolvedJsonPath = null;
            if (!string.IsNullOrWhiteSpace(realmJsonPath))
            {
                // Tenta caminho relativo ao diretório do executável (onde o arquivo é copiado)
                var execDir = AppDomain.CurrentDomain.BaseDirectory;
                var jsonPath = Path.Combine(execDir, realmJsonPath);
                
                if (File.Exists(jsonPath))
                {
                    resolvedJsonPath = Path.GetFullPath(jsonPath);
                    _logger.LogInformation("Arquivo JSON encontrado: {Path}", resolvedJsonPath);
                }
                else
                {
                    // Fallback: tenta relativo ao diretório atual (para desenvolvimento)
                    var currentDir = Directory.GetCurrentDirectory();
                    var currentPath = Path.Combine(currentDir, realmJsonPath);
                    if (File.Exists(currentPath))
                    {
                        resolvedJsonPath = Path.GetFullPath(currentPath);
                        _logger.LogInformation("Arquivo JSON encontrado no diretório atual: {Path}", resolvedJsonPath);
                    }
                    else
                    {
                        _logger.LogError("Arquivo JSON não encontrado. Caminhos tentados:");
                        _logger.LogError("  - {Path} (existe: {Exists})", jsonPath, File.Exists(jsonPath));
                        _logger.LogError("  - {Path} (existe: {Exists})", currentPath, File.Exists(currentPath));
                        _logger.LogError("Certifique-se de que o arquivo está em Config/assistenteexecutivo-realm.json e que o .csproj está configurado para copiá-lo para o output.");
                    }
                }
            }

            if (useJsonImport && !string.IsNullOrWhiteSpace(resolvedJsonPath))
            {
                _logger.LogInformation("Tentando importar realm {RealmId} do arquivo JSON: {JsonPath}", realmId, resolvedJsonPath);
                
                try
                {
                    var importSuccess = await _keycloakService.ImportRealmFromJsonAsync(realmId, resolvedJsonPath, overwriteExisting: true, cancellationToken);
                    
                    if (importSuccess)
                    {
                        _logger.LogInformation("✓ Realm {RealmId} importado/atualizado com sucesso do JSON. Pulando provisionamento manual.", realmId);
                        
                        // Verificar se os Identity Providers foram criados
                        await VerifyIdentityProvidersAsync(realmId, cancellationToken);
                        
                        // Configurar SMTP e rememberMe manualmente (importação parcial pode não aplicar)
                        await ConfigureSmtpAndRememberMeAsync(realmId, resolvedJsonPath, cancellationToken);
                        
                        // Ainda precisa garantir frontend URL e outras configurações dinâmicas
                        // skipProviders=true para não fazer chamadas desnecessárias (providers já estão no JSON)
                        await _keycloakService.CreateRealmAsync(realmId, realmName, skipProviders: true, cancellationToken);
                        _logger.LogInformation("Configurações dinâmicas (frontendUrl, etc.) atualizadas no realm {RealmId}", realmId);
                        return;
                    }
                    else
                    {
                        _logger.LogError("Falha ao importar realm do JSON. Verifique os logs acima para detalhes do erro. Provisionamento manual está desabilitado.");
                        throw new InvalidOperationException($"Falha ao importar realm {realmId} do arquivo JSON: {resolvedJsonPath}. Verifique os logs do KeycloakService para detalhes.");
                    }
                }
                catch (Exception ex) when (ex is not InvalidOperationException)
                {
                    _logger.LogError(ex, "Exceção ao importar realm do JSON. Provisionamento manual está desabilitado.");
                    throw new InvalidOperationException($"Erro ao importar realm {realmId} do arquivo JSON: {resolvedJsonPath}. Erro: {ex.Message}", ex);
                }
            }
            else if (useJsonImport)
            {
                _logger.LogError("UseJsonImport está habilitado mas o arquivo JSON não foi encontrado. Caminho configurado: {JsonPath}.", realmJsonPath);
                throw new FileNotFoundException($"Arquivo JSON do realm não encontrado. Caminho configurado: {realmJsonPath}");
            }
            else
            {
                _logger.LogInformation("Provisionamento via JSON está desabilitado (UseJsonImport=false). Nenhum provisionamento será executado.");
            }
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

    private async Task VerifyIdentityProvidersAsync(string realmId, CancellationToken cancellationToken)
    {
        try
        {
            var adminToken = await GetAdminTokenAsync(cancellationToken);
            var baseUrl = _configuration["Keycloak:BaseUrl"] ?? throw new InvalidOperationException("Keycloak:BaseUrl não configurado em appsettings");
            
            // Verificar quais Identity Providers existem
            var checkRequest = new System.Net.Http.HttpRequestMessage(
                System.Net.Http.HttpMethod.Get,
                $"{baseUrl}/admin/realms/{realmId}/identity-provider/instances");
            checkRequest.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", adminToken);

            using var httpClient = new System.Net.Http.HttpClient();
            var checkResponse = await httpClient.SendAsync(checkRequest, cancellationToken);
            
            if (checkResponse.IsSuccessStatusCode)
            {
                var providers = await checkResponse.Content.ReadFromJsonAsync<List<Dictionary<string, object>>>(cancellationToken: cancellationToken);
                var googleExists = providers?.Any(p => p.ContainsKey("alias") && p["alias"]?.ToString() == "google") ?? false;
                var microsoftExists = providers?.Any(p => p.ContainsKey("alias") && p["alias"]?.ToString() == "microsoft") ?? false;
                
                _logger.LogInformation("Identity Providers verificados - Google: {GoogleExists}, Microsoft: {MicrosoftExists}", googleExists, microsoftExists);
                
                // Se Microsoft não existe, criar manualmente
                if (!microsoftExists)
                {
                    _logger.LogWarning("Microsoft Identity Provider não foi criado pela importação. Criando manualmente...");
                    await CreateMicrosoftIdentityProviderAsync(realmId, adminToken, baseUrl, httpClient, cancellationToken);
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Erro ao verificar Identity Providers no realm {RealmId}", realmId);
        }
    }

    private async Task CreateMicrosoftIdentityProviderAsync(string realmId, string adminToken, string baseUrl, System.Net.Http.HttpClient httpClient, CancellationToken cancellationToken)
    {
        try
        {
            var microsoftIdp = new
            {
                alias = "microsoft",
                providerId = "microsoft",
                enabled = true,
                trustEmail = true,
                storeToken = false,
                addReadTokenRoleOnCreate = false,
                firstBrokerLoginFlowAlias = "first broker login",
                config = new Dictionary<string, string>
                {
                    { "clientId", "6e270dc7-1159-42c0-a4e8-dbc5a029ceb2" },
                    { "clientSecret", "ygZ8Q~5MqVC6NIcfhyF7joSX_oa64iWW8tgHWcPS" },
                    { "defaultScope", "openid profile email User.Read" },
                    { "useJwksUrl", "true" },
                    { "tenant", "common" },
                    { "hideOnLoginPage", "false" },
                    { "acceptsPromptNoneForwardFromClient", "false" },
                    { "disableUserInfo", "false" }
                }
            };

            var createRequest = new System.Net.Http.HttpRequestMessage(
                System.Net.Http.HttpMethod.Post,
                $"{baseUrl}/admin/realms/{realmId}/identity-provider/instances");
            createRequest.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", adminToken);
            createRequest.Content = System.Net.Http.Json.JsonContent.Create(microsoftIdp);

            var createResponse = await httpClient.SendAsync(createRequest, cancellationToken);
            
            if (createResponse.IsSuccessStatusCode)
            {
                _logger.LogInformation("✓ Microsoft Identity Provider criado com sucesso no realm {RealmId}", realmId);
                
                // Criar mappers para Microsoft
                await CreateMicrosoftMappersAsync(realmId, adminToken, baseUrl, httpClient, cancellationToken);
            }
            else
            {
                var errorContent = await createResponse.Content.ReadAsStringAsync(cancellationToken);
                _logger.LogWarning("Erro ao criar Microsoft Identity Provider. Status: {Status}, Response: {Response}",
                    createResponse.StatusCode, errorContent);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao criar Microsoft Identity Provider no realm {RealmId}", realmId);
        }
    }

    private async Task CreateMicrosoftMappersAsync(string realmId, string adminToken, string baseUrl, System.Net.Http.HttpClient httpClient, CancellationToken cancellationToken)
    {
        var mappers = new[]
        {
            new { name = "microsoft-email", identityProviderAlias = "microsoft", identityProviderMapper = "oidc-user-attribute-idp-mapper", config = new Dictionary<string, string> { { "claim", "email" }, { "user.attribute", "email" }, { "syncMode", "INHERIT" } } },
            new { name = "microsoft-given-name", identityProviderAlias = "microsoft", identityProviderMapper = "oidc-user-attribute-idp-mapper", config = new Dictionary<string, string> { { "claim", "given_name" }, { "user.attribute", "firstName" }, { "syncMode", "INHERIT" } } },
            new { name = "microsoft-family-name", identityProviderAlias = "microsoft", identityProviderMapper = "oidc-user-attribute-idp-mapper", config = new Dictionary<string, string> { { "claim", "family_name" }, { "user.attribute", "lastName" }, { "syncMode", "INHERIT" } } },
            new { name = "microsoft-name", identityProviderAlias = "microsoft", identityProviderMapper = "oidc-user-attribute-idp-mapper", config = new Dictionary<string, string> { { "claim", "name" }, { "user.attribute", "name" }, { "syncMode", "INHERIT" } } }
        };

        foreach (var mapper in mappers)
        {
            try
            {
                var createRequest = new System.Net.Http.HttpRequestMessage(
                    System.Net.Http.HttpMethod.Post,
                    $"{baseUrl}/admin/realms/{realmId}/identity-provider/instances/microsoft/mappers");
                createRequest.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", adminToken);
                createRequest.Content = System.Net.Http.Json.JsonContent.Create(mapper);

                var createResponse = await httpClient.SendAsync(createRequest, cancellationToken);
                
                if (createResponse.IsSuccessStatusCode)
                {
                    _logger.LogInformation("Mapper {MapperName} criado para Microsoft Identity Provider", mapper.name);
                }
                else if (createResponse.StatusCode == System.Net.HttpStatusCode.Conflict)
                {
                    _logger.LogInformation("Mapper {MapperName} já existe para Microsoft Identity Provider", mapper.name);
                }
                else
                {
                    var errorContent = await createResponse.Content.ReadAsStringAsync(cancellationToken);
                    _logger.LogWarning("Erro ao criar mapper {MapperName}. Status: {Status}, Response: {Response}",
                        mapper.name, createResponse.StatusCode, errorContent);
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Erro ao criar mapper {MapperName} para Microsoft Identity Provider", mapper.name);
            }
        }
    }

    private async Task ConfigureSmtpAndRememberMeAsync(string realmId, string jsonFilePath, CancellationToken cancellationToken)
    {
        try
        {
            var adminToken = await GetAdminTokenAsync(cancellationToken);
            var baseUrl = _configuration["Keycloak:BaseUrl"] ?? throw new InvalidOperationException("Keycloak:BaseUrl não configurado em appsettings");
            
            // Ler o JSON para extrair configurações SMTP e rememberMe
            var jsonContent = await File.ReadAllTextAsync(jsonFilePath, cancellationToken);
            var jsonDoc = System.Text.Json.JsonDocument.Parse(jsonContent);
            var realmJson = jsonDoc.RootElement;
            
            // Obter configuração atual do realm
            using var httpClient = new System.Net.Http.HttpClient();
            var getRequest = new System.Net.Http.HttpRequestMessage(
                System.Net.Http.HttpMethod.Get,
                $"{baseUrl}/admin/realms/{realmId}");
            getRequest.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", adminToken);
            
            var getResponse = await httpClient.SendAsync(getRequest, cancellationToken);
            if (!getResponse.IsSuccessStatusCode)
            {
                _logger.LogWarning("Não foi possível obter configuração do realm {RealmId} para configurar SMTP e rememberMe", realmId);
                return;
            }
            
            var realmJsonString = await getResponse.Content.ReadAsStringAsync(cancellationToken);
            var realmDict = System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, System.Text.Json.JsonElement>>(realmJsonString)
                ?? new Dictionary<string, System.Text.Json.JsonElement>();
            
            // Preparar payload de atualização
            var updatePayload = new Dictionary<string, object>();
            foreach (var kvp in realmDict)
            {
                if (kvp.Key == "id")
                    continue;
                
                object? value = kvp.Value.ValueKind switch
                {
                    System.Text.Json.JsonValueKind.String => kvp.Value.GetString() ?? string.Empty,
                    System.Text.Json.JsonValueKind.Number => kvp.Value.TryGetInt32(out var intVal) ? intVal : (object)kvp.Value.GetDouble(),
                    System.Text.Json.JsonValueKind.True => true,
                    System.Text.Json.JsonValueKind.False => false,
                    System.Text.Json.JsonValueKind.Null => null,
                    _ => kvp.Value
                };
                
                if (value != null)
                    updatePayload[kvp.Key] = value;
            }
            
            // Configurar rememberMe do JSON
            if (realmJson.TryGetProperty("rememberMe", out var rememberMeProp))
            {
                updatePayload["rememberMe"] = rememberMeProp.GetBoolean();
                _logger.LogInformation("Configurando rememberMe: {RememberMe}", rememberMeProp.GetBoolean());
            }
            
            // Configurar SMTP do JSON
            if (realmJson.TryGetProperty("smtpServer", out var smtpServerProp))
            {
                var smtpConfig = new Dictionary<string, object>();
                foreach (var prop in smtpServerProp.EnumerateObject())
                {
                    var propValue = prop.Value.ValueKind switch
                    {
                        System.Text.Json.JsonValueKind.String => prop.Value.GetString() ?? string.Empty,
                        System.Text.Json.JsonValueKind.True => "true",
                        System.Text.Json.JsonValueKind.False => "false",
                        System.Text.Json.JsonValueKind.Number => prop.Value.GetInt32().ToString(),
                        _ => prop.Value.GetString() ?? string.Empty
                    };
                    smtpConfig[prop.Name] = propValue;
                }
                updatePayload["smtpServer"] = smtpConfig;
                _logger.LogInformation("Configurando SMTP: {Host}:{Port}", smtpConfig.GetValueOrDefault("host"), smtpConfig.GetValueOrDefault("port"));
            }
            
            // Atualizar realm
            var updateRequest = new System.Net.Http.HttpRequestMessage(
                System.Net.Http.HttpMethod.Put,
                $"{baseUrl}/admin/realms/{realmId}");
            updateRequest.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", adminToken);
            updateRequest.Content = System.Net.Http.Json.JsonContent.Create(updatePayload);
            
            var updateResponse = await httpClient.SendAsync(updateRequest, cancellationToken);
            if (updateResponse.IsSuccessStatusCode)
            {
                _logger.LogInformation("✓ SMTP e rememberMe configurados no realm {RealmId}", realmId);
            }
            else
            {
                var errorContent = await updateResponse.Content.ReadAsStringAsync(cancellationToken);
                _logger.LogWarning("Erro ao configurar SMTP e rememberMe no realm {RealmId}. Status: {Status}, Response: {Response}",
                    realmId, updateResponse.StatusCode, errorContent);
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Erro ao configurar SMTP e rememberMe no realm {RealmId}", realmId);
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

        var tokenUrl = $"{keycloakBaseUrl}/realms/{adminRealm}/protocol/openid-connect/token";
        _logger.LogDebug("Tentando obter token de admin do Keycloak em {TokenUrl}", tokenUrl);

        using var httpClient = new System.Net.Http.HttpClient();
        var request = new System.Net.Http.HttpRequestMessage(
            System.Net.Http.HttpMethod.Post,
            tokenUrl)
        {
            Content = new System.Net.Http.FormUrlEncodedContent(tokenRequest)
        };

        try
        {
            var response = await httpClient.SendAsync(request, cancellationToken);
            var responseContent = await response.Content.ReadAsStringAsync(cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogError(
                    "Falha ao obter token de admin do Keycloak. Status: {StatusCode}, Response: {Response}",
                    response.StatusCode,
                    responseContent);
                throw new Exception($"Falha ao obter token de admin do Keycloak. Status: {response.StatusCode}, Response: {responseContent}");
            }

            KeycloakTokenResponse? tokenResponse;
            try
            {
                tokenResponse = JsonSerializer.Deserialize<KeycloakTokenResponse>(responseContent);
            }
            catch (JsonException ex)
            {
                _logger.LogError(ex, "Erro ao deserializar resposta do token de admin. Response: {Response}", responseContent);
                throw new Exception($"Erro ao deserializar resposta do token de admin: {ex.Message}. Response: {responseContent}", ex);
            }

            if (tokenResponse == null || string.IsNullOrWhiteSpace(tokenResponse.AccessToken))
            {
                _logger.LogError("Token de admin não retornado na resposta. Response: {Response}", responseContent);
                throw new Exception($"Token de admin não retornado. Response: {responseContent}");
            }

            _logger.LogDebug("Token de admin obtido com sucesso");
            return tokenResponse.AccessToken;
        }
        catch (System.Net.Http.HttpRequestException ex)
        {
            _logger.LogError(ex, "Erro de rede ao obter token de admin do Keycloak");
            throw new Exception($"Erro de rede ao obter token de admin do Keycloak: {ex.Message}", ex);
        }
    }

    private class KeycloakTokenResponse
    {
        [System.Text.Json.Serialization.JsonPropertyName("access_token")]
        public string AccessToken { get; set; } = string.Empty;
        
        [System.Text.Json.Serialization.JsonPropertyName("expires_in")]
        public int ExpiresIn { get; set; }
    }
}

