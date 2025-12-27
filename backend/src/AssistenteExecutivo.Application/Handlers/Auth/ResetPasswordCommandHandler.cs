using AssistenteExecutivo.Application.Commands.Auth;
using AssistenteExecutivo.Application.Interfaces;
using AssistenteExecutivo.Application.Queries.Auth;
using AssistenteExecutivo.Domain.Interfaces;
using AssistenteExecutivo.Domain.ValueObjects;
using MediatR;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace AssistenteExecutivo.Application.Handlers.Auth;

public class ResetPasswordCommandHandler : IRequestHandler<ResetPasswordCommand, ResetPasswordResult>
{
    private readonly IMediator _mediator;
    private readonly IUserProfileRepository _userProfileRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IKeycloakService _keycloakService;
    private readonly IClock _clock;
    private readonly IConfiguration _configuration;
    private readonly ILogger<ResetPasswordCommandHandler> _logger;

    public ResetPasswordCommandHandler(
        IMediator mediator,
        IUserProfileRepository userProfileRepository,
        IUnitOfWork unitOfWork,
        IKeycloakService keycloakService,
        IClock clock,
        IConfiguration configuration,
        ILogger<ResetPasswordCommandHandler> logger)
    {
        _mediator = mediator;
        _userProfileRepository = userProfileRepository;
        _unitOfWork = unitOfWork;
        _keycloakService = keycloakService;
        _clock = clock;
        _configuration = configuration;
        _logger = logger;
    }

    public async Task<ResetPasswordResult> Handle(ResetPasswordCommand request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.Email)
            || string.IsNullOrWhiteSpace(request.Token)
            || string.IsNullOrWhiteSpace(request.NewPassword))
        {
            return new ResetPasswordResult
            {
                Success = false,
                ErrorMessage = "Requisição inválida."
            };
        }

        EmailAddress email;
        try
        {
            email = EmailAddress.Create(request.Email);
        }
        catch
        {
            return new ResetPasswordResult
            {
                Success = false,
                ErrorMessage = "Email inválido."
            };
        }

        if (request.NewPassword.Trim().Length < 8)
        {
            return new ResetPasswordResult
            {
                Success = false,
                ErrorMessage = "Senha inválida (mínimo 8 caracteres)."
            };
        }

        var user = await _mediator.Send(new GetUserByEmailQuery { Email = email.Value }, cancellationToken);

        if (user == null || !user.ValidatePasswordResetToken(request.Token.Trim(), _clock))
        {
            return new ResetPasswordResult
            {
                Success = false,
                ErrorMessage = "Token inválido ou expirado."
            };
        }

        var realmId = _configuration["Keycloak:Realm"] ?? "assistenteexecutivo";

        try
        {
            await _keycloakService.UpdateUserPasswordAsync(
                realmId,
                user.KeycloakSubject.Value,
                request.NewPassword,
                cancellationToken);

            user.InvalidatePasswordResetToken();
            await _userProfileRepository.UpdateAsync(user, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            return new ResetPasswordResult
            {
                Success = true
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao atualizar senha no Keycloak para {Email}", email.Value);
            return new ResetPasswordResult
            {
                Success = false,
                ErrorMessage = "Erro ao atualizar senha. Tente novamente mais tarde."
            };
        }
    }
}





