namespace AssistenteExecutivo.Application.Interfaces;

/// <summary>
/// Service responsável por provisionar automaticamente o Keycloak (realm, clients, roles, usuários dev/teste).
/// Executado apenas em DEV/HML no startup da aplicação.
/// </summary>
public interface IKeycloakAdminProvisioner
{
    /// <summary>
    /// Executa o bootstrap completo do Keycloak de forma idempotente.
    /// </summary>
    Task ProvisionAsync(CancellationToken cancellationToken = default);
}

