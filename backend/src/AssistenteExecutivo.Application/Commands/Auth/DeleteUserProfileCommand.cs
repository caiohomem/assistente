using MediatR;

namespace AssistenteExecutivo.Application.Commands.Auth;

public class DeleteUserProfileCommand : IRequest<Unit>
{
    public Guid UserId { get; set; }
}





