using AssistenteExecutivo.Application.Commands.Auth;
using AssistenteExecutivo.Application.Interfaces;
using AssistenteExecutivo.Domain.Entities;
using AssistenteExecutivo.Domain.Enums;
using AssistenteExecutivo.Domain.Exceptions;
using AssistenteExecutivo.Domain.Interfaces;
using AssistenteExecutivo.Domain.Notifications;
using AssistenteExecutivo.Domain.ValueObjects;
using MediatR;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace AssistenteExecutivo.Application.Handlers.Auth;

public class ReactivateDeletedUserCommandHandler : IRequestHandler<ReactivateDeletedUserCommand, ReactivateDeletedUserResult>
{
    private readonly IUserProfileRepository _userProfileRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IClock _clock;
    private readonly ILogger<ReactivateDeletedUserCommandHandler> _logger;
    private readonly IEmailService _emailService;
    private readonly IConfiguration _configuration;

    public ReactivateDeletedUserCommandHandler(
        IUserProfileRepository userProfileRepository,
        IUnitOfWork unitOfWork,
        IClock clock,
        ILogger<ReactivateDeletedUserCommandHandler> logger,
        IEmailService emailService,
        IConfiguration configuration)
    {
        _userProfileRepository = userProfileRepository;
        _unitOfWork = unitOfWork;
        _clock = clock;
        _logger = logger;
        _emailService = emailService;
        _configuration = configuration;
    }

    public async Task<ReactivateDeletedUserResult> Handle(ReactivateDeletedUserCommand request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.Email))
            throw new ArgumentException("Email é obrigatório", nameof(request));

        if (string.IsNullOrWhiteSpace(request.KeycloakSubject))
            throw new ArgumentException("KeycloakSubject é obrigatório", nameof(request));

        var email = EmailAddress.Create(request.Email);
        var keycloakSubject = KeycloakSubject.Create(request.KeycloakSubject);

        var existingUser = await _userProfileRepository.GetByEmailAsync(request.Email, cancellationToken);

        if (existingUser == null)
        {
            _logger.LogWarning("Tentativa de reativar usuário que não existe. Email={Email}", request.Email);
            throw new DomainException("Domain:UsuarioNaoEncontrado");
        }

        if (existingUser.Status != UserStatus.Deleted)
        {
            _logger.LogWarning("Tentativa de reativar usuário que não está deletado. Email={Email}, Status={Status}", 
                request.Email, existingUser.Status);
            throw new DomainException("Domain:ApenasUsuariosDeletadosPodemSerReativados");
        }

        // Se o KeycloakSubject for diferente, atualizar (usuário pode ter criado nova conta no Keycloak)
        if (existingUser.KeycloakSubject.Value != keycloakSubject.Value)
        {
            _logger.LogInformation(
                "Atualizando KeycloakSubject durante reativação. Email={Email}, OldSubject={OldSubject}, NewSubject={NewSubject}",
                request.Email, existingUser.KeycloakSubject.Value, keycloakSubject.Value);
            existingUser.ReactivateWithNewKeycloakSubject(keycloakSubject, _clock);
        }
        else
        {
            existingUser.Reactivate(_clock);
        }
        await _userProfileRepository.UpdateAsync(existingUser, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Usuário reativado com sucesso. Email={Email}, UserId={UserId}", 
            request.Email, existingUser.UserId);

        // Enviar email de boas-vindas
        try
        {
            var appUrl = _configuration["Frontend:BaseUrl"] ?? _configuration["App:BaseUrl"] ?? "https://app.assistenteexecutivo.com";
            var supportUrl = _configuration["Frontend:SupportUrl"] ?? $"{appUrl}/suporte";
            var privacyUrl = _configuration["Frontend:PrivacyUrl"] ?? $"{appUrl}/privacidade";

            var fullName = existingUser.DisplayName.FullName;
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
            // Não falhar a reativação se o email falhar, apenas logar o erro
            _logger.LogError(ex, "Erro ao enviar email de boas-vindas para {Email}. O usuário foi reativado com sucesso.", request.Email);
        }

        return new ReactivateDeletedUserResult
        {
            UserId = existingUser.UserId,
            Email = request.Email
        };
    }
}

