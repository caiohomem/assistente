using MediatR;

namespace AssistenteExecutivo.Application.Commands.Auth;

/// <summary>
/// Command para gerar token de reset de senha para um usuário.
/// </summary>
public class GeneratePasswordResetTokenCommand : IRequest<GeneratePasswordResetTokenResult>
{
    /// <summary>
    /// Email do usuário.
    /// </summary>
    public string Email { get; set; } = string.Empty;
}

/// <summary>
/// Resultado da geração do token de reset de senha.
/// </summary>
public class GeneratePasswordResetTokenResult
{
    /// <summary>
    /// Indica se o token foi gerado com sucesso.
    /// </summary>
    public bool Success { get; set; }

    /// <summary>
    /// Email do usuário.
    /// </summary>
    public string Email { get; set; } = string.Empty;

    /// <summary>
    /// Nome completo do usuário (para uso em templates de email).
    /// </summary>
    public string FullName { get; set; } = string.Empty;

    /// <summary>
    /// Token gerado (apenas se Success for true).
    /// </summary>
    public string? Token { get; set; }
}










