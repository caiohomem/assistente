using AssistenteExecutivo.Application.Interfaces;
using AssistenteExecutivo.Application.Queries.Auth;
using AssistenteExecutivo.Domain.Entities;
using MediatR;

namespace AssistenteExecutivo.Application.Handlers.Auth;

public class GetUserByEmailQueryHandler : IRequestHandler<GetUserByEmailQuery, UserProfile?>
{
    private readonly IUserProfileRepository _userProfileRepository;

    public GetUserByEmailQueryHandler(IUserProfileRepository userProfileRepository)
    {
        _userProfileRepository = userProfileRepository;
    }

    public async Task<UserProfile?> Handle(GetUserByEmailQuery request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.Email))
            return null;

        try
        {
            return await _userProfileRepository.GetByEmailAsync(request.Email, cancellationToken);
        }
        catch
        {
            // Se EmailAddress.Create falhar ou qualquer outro erro, retornar null
            return null;
        }
    }
}





