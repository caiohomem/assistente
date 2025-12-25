using MediatR;

namespace AssistenteExecutivo.Application.Commands.Auth;

public class RegisterUserCommand : IRequest<RegisterUserResult>
{
    public string Email { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
}

public class RegisterUserResult
{
    public Guid UserId { get; set; }
    public string Email { get; set; } = string.Empty;
    public string RealmId { get; set; } = string.Empty;
}


