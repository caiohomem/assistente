using AssistenteExecutivo.Application.Commands.Auth;
using AssistenteExecutivo.Application.Interfaces;
using AssistenteExecutivo.Domain.Exceptions;
using MediatR;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace AssistenteExecutivo.Application.Handlers.Auth;

public class DeleteUserProfileCommandHandler : IRequestHandler<DeleteUserProfileCommand, Unit>
{
    private readonly IUserProfileRepository _userProfileRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IKeycloakService _keycloakService;
    private readonly IConfiguration _configuration;
    private readonly ILogger<DeleteUserProfileCommandHandler> _logger;

    public DeleteUserProfileCommandHandler(
        IUserProfileRepository userProfileRepository,
        IUnitOfWork unitOfWork,
        IKeycloakService keycloakService,
        IConfiguration configuration,
        ILogger<DeleteUserProfileCommandHandler> logger)
    {
        _userProfileRepository = userProfileRepository;
        _unitOfWork = unitOfWork;
        _keycloakService = keycloakService;
        _configuration = configuration;
        _logger = logger;
    }

    public async Task<Unit> Handle(DeleteUserProfileCommand request, CancellationToken cancellationToken)
    {
        if (request.UserId == Guid.Empty)
            throw new DomainException("Domain:UserIdObrigatorio");

        var userProfile = await _userProfileRepository.GetByIdAsync(request.UserId, cancellationToken);

        if (userProfile == null)
            throw new DomainException("Domain:UsuarioNaoEncontrado");

        if (userProfile.Status == Domain.Enums.UserStatus.Deleted)
        {
            _logger.LogWarning("Tentativa de deletar usuário já deletado: {UserId}", request.UserId);
            return Unit.Value;
        }

        // Deletar usuário no Keycloak
        var realm = _configuration["Keycloak:Realm"] ?? "assistenteexecutivo";
        var keycloakUserId = userProfile.KeycloakSubject.Value;

        try
        {
            await _keycloakService.DeleteUserAsync(realm, keycloakUserId, cancellationToken);
            _logger.LogInformation("Usuário {KeycloakUserId} deletado do Keycloak para UserProfile {UserId}",
                keycloakUserId, request.UserId);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Erro ao deletar usuário {KeycloakUserId} do Keycloak. Continuando com exclusão do perfil.",
                keycloakUserId);
            // Continuar mesmo se falhar - perfil será deletado do banco
        }

        // Marcar como deletado (soft delete)
        userProfile.Delete();
        await _userProfileRepository.UpdateAsync(userProfile, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("UserProfile {UserId} deletado com sucesso", request.UserId);

        return Unit.Value;
    }
}




