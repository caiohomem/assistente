using MediatR;

namespace AssistenteExecutivo.Application.Queries.Auth;

/// <summary>
/// Query para obter o UserId (OwnerUserId) do usuário autenticado a partir do KeycloakSubject.
/// </summary>
public class GetOwnerUserIdQuery : IRequest<Guid?>
{
    /// <summary>
    /// O KeycloakSubject (sub claim) do usuário autenticado.
    /// </summary>
    public string KeycloakSubject { get; set; } = string.Empty;
}


