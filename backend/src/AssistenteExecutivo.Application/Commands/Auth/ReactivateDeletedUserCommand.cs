using MediatR;

namespace AssistenteExecutivo.Application.Commands.Auth;

/// <summary>
/// Command para reativar um usuário deletado.
/// </summary>
public class ReactivateDeletedUserCommand : IRequest<ReactivateDeletedUserResult>
{
    /// <summary>
    /// Email do usuário a ser reativado.
    /// </summary>
    public string Email { get; set; } = string.Empty;

    /// <summary>
    /// KeycloakSubject do usuário (sub claim).
    /// </summary>
    public string KeycloakSubject { get; set; } = string.Empty;
}

/// <summary>
/// Resultado da reativação de usuário.
/// </summary>
public class ReactivateDeletedUserResult
{
    /// <summary>
    /// UserId do usuário reativado.
    /// </summary>
    public Guid UserId { get; set; }

    /// <summary>
    /// Email do usuário.
    /// </summary>
    public string Email { get; set; } = string.Empty;
}

