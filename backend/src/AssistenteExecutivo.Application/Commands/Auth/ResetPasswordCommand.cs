using MediatR;

namespace AssistenteExecutivo.Application.Commands.Auth;

/// <summary>
/// Command para resetar a senha de um usuário usando token.
/// </summary>
public class ResetPasswordCommand : IRequest<ResetPasswordResult>
{
    /// <summary>
    /// Email do usuário.
    /// </summary>
    public string Email { get; set; } = string.Empty;

    /// <summary>
    /// Token de reset de senha.
    /// </summary>
    public string Token { get; set; } = string.Empty;

    /// <summary>
    /// Nova senha.
    /// </summary>
    public string NewPassword { get; set; } = string.Empty;
}

/// <summary>
/// Resultado do reset de senha.
/// </summary>
public class ResetPasswordResult
{
    /// <summary>
    /// Indica se o reset foi bem-sucedido.
    /// </summary>
    public bool Success { get; set; }

    /// <summary>
    /// Mensagem de erro (se Success for false).
    /// </summary>
    public string? ErrorMessage { get; set; }
}





