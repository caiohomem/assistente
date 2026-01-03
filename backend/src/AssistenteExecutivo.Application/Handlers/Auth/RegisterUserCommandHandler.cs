using AssistenteExecutivo.Application.Commands.Auth;
using AssistenteExecutivo.Application.Interfaces;
using AssistenteExecutivo.Domain.Constants;
using AssistenteExecutivo.Domain.Entities;
using AssistenteExecutivo.Domain.Interfaces;
using AssistenteExecutivo.Domain.Notifications;
using AssistenteExecutivo.Domain.ValueObjects;
using MediatR;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace AssistenteExecutivo.Application.Handlers.Auth;

public class RegisterUserCommandHandler : IRequestHandler<RegisterUserCommand, RegisterUserResult>
{
    private readonly IUserProfileRepository _userProfileRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IKeycloakService _keycloakService;
    private readonly IClock _clock;
    private readonly IConfiguration _configuration;
    private readonly ILogger<RegisterUserCommandHandler> _logger;
    private readonly IEmailService _emailService;
    private readonly IRelationshipTypeRepository _relationshipTypeRepository;

    public RegisterUserCommandHandler(
        IUserProfileRepository userProfileRepository,
        IUnitOfWork unitOfWork,
        IKeycloakService keycloakService,
        IClock clock,
        IConfiguration configuration,
        ILogger<RegisterUserCommandHandler> logger,
        IEmailService emailService,
        IRelationshipTypeRepository relationshipTypeRepository)
    {
        _userProfileRepository = userProfileRepository;
        _unitOfWork = unitOfWork;
        _keycloakService = keycloakService;
        _clock = clock;
        _configuration = configuration;
        _logger = logger;
        _emailService = emailService;
        _relationshipTypeRepository = relationshipTypeRepository;
    }

    public async Task<RegisterUserResult> Handle(RegisterUserCommand request, CancellationToken cancellationToken)
    {
        // Validar email único
        var existingUser = await _userProfileRepository.GetByEmailAsync(request.Email, cancellationToken);

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

        await _userProfileRepository.AddAsync(userProfile, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        await EnsureDefaultRelationshipTypesAsync(userId, cancellationToken);

        _logger.LogInformation("UserProfile criado com sucesso para {Email} (UserId={UserId}, KeycloakSubject={KeycloakSubject})",
            request.Email, userId, keycloakSubject.Value);

        // Enviar email de boas-vindas
        try
        {
            var appUrl = _configuration["Frontend:BaseUrl"] ?? _configuration["App:BaseUrl"] ?? "https://app.assistenteexecutivo.com";
            var supportUrl = _configuration["Frontend:SupportUrl"] ?? $"{appUrl}/suporte";
            var privacyUrl = _configuration["Frontend:PrivacyUrl"] ?? $"{appUrl}/privacidade";

            var fullName = $"{request.FirstName} {request.LastName}".Trim();
            if (string.IsNullOrWhiteSpace(fullName))
            {
                fullName = request.Email;
            }

            var templateValues = new Dictionary<string, object>
            {
                { "NomeUsuario", fullName },
                { "AppUrl", appUrl },
                { "SupportUrl", supportUrl },
                { "PrivacyUrl", privacyUrl }
            };

            await _emailService.SendEmailWithTemplateAsync(
                EmailTemplateType.UserCreated,
                request.Email,
                fullName,
                templateValues,
                cancellationToken);

            _logger.LogInformation("Email de boas-vindas enviado com sucesso para {Email}", request.Email);
        }
        catch (Exception ex)
        {
            // Não falhar o registro se o email falhar, apenas logar o erro
            _logger.LogError(ex, "Erro ao enviar email de boas-vindas para {Email}. O usuário foi criado com sucesso.", request.Email);
        }

        return new RegisterUserResult
        {
            UserId = userId,
            Email = request.Email,
            RealmId = realmId
        };
    }

    private async Task EnsureDefaultRelationshipTypesAsync(Guid ownerUserId, CancellationToken cancellationToken)
    {
        var existing = await _relationshipTypeRepository.GetByOwnerAsync(ownerUserId, cancellationToken);
        if (existing.Count > 0)
            return;

        var defaults = RelationshipTypeDefaults.Names
            .Select(name => new RelationshipType(Guid.NewGuid(), ownerUserId, name, _clock, isDefault: true))
            .ToList();

        await _relationshipTypeRepository.AddRangeAsync(defaults, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }
}
