using AssistenteExecutivo.Application.Commands.Auth;
using AssistenteExecutivo.Application.Interfaces;
using AssistenteExecutivo.Domain.Entities;
using AssistenteExecutivo.Domain.Interfaces;
using AssistenteExecutivo.Domain.ValueObjects;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace AssistenteExecutivo.Application.Handlers.Auth;

public class RegisterUserCommandHandler : IRequestHandler<RegisterUserCommand, RegisterUserResult>
{
    private readonly IApplicationDbContext _db;
    private readonly IKeycloakService _keycloakService;
    private readonly IClock _clock;
    private readonly IConfiguration _configuration;
    private readonly ILogger<RegisterUserCommandHandler> _logger;

    public RegisterUserCommandHandler(
        IApplicationDbContext db,
        IKeycloakService keycloakService,
        IClock clock,
        IConfiguration configuration,
        ILogger<RegisterUserCommandHandler> logger)
    {
        _db = db;
        _keycloakService = keycloakService;
        _clock = clock;
        _configuration = configuration;
        _logger = logger;
    }

    public async Task<RegisterUserResult> Handle(RegisterUserCommand request, CancellationToken cancellationToken)
    {
        // Validar email único
        var normalizedEmail = EmailAddress.Create(request.Email).Value;
        var existingUser = await _db.UserProfiles
            .FirstOrDefaultAsync(u => u.Email.Value == normalizedEmail, cancellationToken);

        if (existingUser != null)
        {
            throw new InvalidOperationException("Este email já está cadastrado. Faça login em vez de criar uma nova conta.");
        }

        // Obter realm do Keycloak
        var realmId = _configuration["Keycloak:Realm"] ?? "assistenteexecutivo";

        // Verificar se usuário já existe no Keycloak
        var keycloakUserId = await _keycloakService.GetUserIdByEmailAsync(realmId, request.Email, cancellationToken);

        if (string.IsNullOrEmpty(keycloakUserId))
        {
            // Criar usuário no Keycloak
            keycloakUserId = await _keycloakService.CreateUserAsync(
                realmId,
                request.Email,
                request.FirstName,
                request.LastName,
                request.Password,
                cancellationToken);

            _logger.LogInformation("Usuário {Email} criado no Keycloak com ID {UserId}", request.Email, keycloakUserId);
        }
        else
        {
            _logger.LogInformation("Usuário {Email} já existe no Keycloak com ID {UserId}", request.Email, keycloakUserId);
        }

        // Criar UserProfile no banco de dados
        var userId = Guid.NewGuid();
        var keycloakSubject = KeycloakSubject.Create(keycloakUserId);
        var email = EmailAddress.Create(request.Email);
        var displayName = PersonName.Create(request.FirstName, request.LastName);

        var userProfile = new UserProfile(
            userId: userId,
            keycloakSubject: keycloakSubject,
            email: email,
            displayName: displayName,
            clock: _clock);

        _db.UserProfiles.Add(userProfile);
        await _db.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("UserProfile criado com sucesso para {Email} (UserId={UserId}, KeycloakSubject={KeycloakSubject})",
            request.Email, userId, keycloakSubject.Value);

        return new RegisterUserResult
        {
            UserId = userId,
            Email = request.Email,
            RealmId = realmId
        };
    }
}

