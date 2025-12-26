using MediatR;

namespace AssistenteExecutivo.Application.Commands.Auth;

/// <summary>
/// Command para criar ou atualizar UserProfile automaticamente durante OAuth callback.
/// </summary>
public class ProvisionUserFromKeycloakCommand : IRequest<ProvisionUserFromKeycloakResult>
{
    /// <summary>
    /// O KeycloakSubject (sub claim) do usuário autenticado.
    /// </summary>
    public string KeycloakSubject { get; set; } = string.Empty;

    /// <summary>
    /// Email do usuário obtido do Keycloak.
    /// </summary>
    public string Email { get; set; } = string.Empty;

    /// <summary>
    /// Primeiro nome do usuário (GivenName ou extraído de Name).
    /// </summary>
    public string? FirstName { get; set; }

    /// <summary>
    /// Sobrenome do usuário (FamilyName ou extraído de Name).
    /// </summary>
    public string? LastName { get; set; }

    /// <summary>
    /// Nome completo do usuário (Name do Keycloak).
    /// </summary>
    public string? FullName { get; set; }
}

/// <summary>
/// Resultado do provisionamento de usuário.
/// </summary>
public class ProvisionUserFromKeycloakResult
{
    /// <summary>
    /// UserId do usuário provisionado ou existente.
    /// </summary>
    public Guid UserId { get; set; }

    /// <summary>
    /// Indica se o usuário foi criado (true) ou já existia (false).
    /// </summary>
    public bool WasCreated { get; set; }

    /// <summary>
    /// Email do usuário.
    /// </summary>
    public string Email { get; set; } = string.Empty;
}



