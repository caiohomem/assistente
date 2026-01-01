using AssistenteExecutivo.Domain.Entities;
using MediatR;

namespace AssistenteExecutivo.Application.Queries.Auth;

/// <summary>
/// Query para obter um UserProfile pelo email.
/// </summary>
public class GetUserByEmailQuery : IRequest<UserProfile?>
{
    /// <summary>
    /// Email do usuário a ser buscado.
    /// </summary>
    public string Email { get; set; } = string.Empty;
}












