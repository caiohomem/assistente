using AssistenteExecutivo.Application.Interfaces;
using AssistenteExecutivo.Application.Queries.Auth;
using MediatR;
namespace AssistenteExecutivo.Application.Handlers.Auth;

public class GetOwnerUserIdQueryHandler : IRequestHandler<GetOwnerUserIdQuery, Guid?>
{
    private readonly IUserProfileRepository _userProfileRepository;

    public GetOwnerUserIdQueryHandler(IUserProfileRepository userProfileRepository)
    {
        _userProfileRepository = userProfileRepository;
    }

    public async Task<Guid?> Handle(GetOwnerUserIdQuery request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.KeycloakSubject))
        {
            return null;
        }

        try
        {
            var userProfile = await _userProfileRepository.GetByKeycloakSubjectAsync(request.KeycloakSubject, cancellationToken);

            return userProfile?.UserId;
        }
        catch (Exception ex)
        {
            // Se KeycloakSubject.Create falhar ou qualquer outro erro, retornar null
            return null;
        }
    }
}

