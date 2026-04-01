using MediatR;

namespace AssistenteExecutivo.Application.Queries.Auth;

/// <summary>
/// Query para obter ou provisionar o UserId (OwnerUserId) do usuário autenticado.
/// </summary>
public class GetOwnerUserIdQuery : IRequest<Guid?>
{
    public string KeycloakSubject { get; set; } = string.Empty;
    public string? Email { get; set; }
    public string? DisplayName { get; set; }
}













