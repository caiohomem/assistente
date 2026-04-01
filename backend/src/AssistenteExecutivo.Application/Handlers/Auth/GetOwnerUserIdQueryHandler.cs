using AssistenteExecutivo.Application.Interfaces;
using AssistenteExecutivo.Application.Queries.Auth;
using AssistenteExecutivo.Domain.Entities;
using AssistenteExecutivo.Domain.Interfaces;
using AssistenteExecutivo.Domain.ValueObjects;
using MediatR;
using Microsoft.Extensions.Logging;

namespace AssistenteExecutivo.Application.Handlers.Auth;

public class GetOwnerUserIdQueryHandler : IRequestHandler<GetOwnerUserIdQuery, Guid?>
{
    private readonly IUserProfileRepository _userProfileRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IClock _clock;
    private readonly ILogger<GetOwnerUserIdQueryHandler> _logger;

    public GetOwnerUserIdQueryHandler(
        IUserProfileRepository userProfileRepository,
        IUnitOfWork unitOfWork,
        IClock clock,
        ILogger<GetOwnerUserIdQueryHandler> logger)
    {
        _userProfileRepository = userProfileRepository;
        _unitOfWork = unitOfWork;
        _clock = clock;
        _logger = logger;
    }

    public async Task<Guid?> Handle(GetOwnerUserIdQuery request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.KeycloakSubject))
        {
            return null;
        }

        try
        {
            var existingUser = await _userProfileRepository.GetByKeycloakSubjectAsync(request.KeycloakSubject, cancellationToken);
            if (existingUser != null)
            {
                return existingUser.UserId;
            }

            if (!string.IsNullOrWhiteSpace(request.Email))
            {
                existingUser = await _userProfileRepository.GetByEmailAsync(request.Email, cancellationToken);
                if (existingUser != null)
                {
                    return existingUser.UserId;
                }
            }

            if (string.IsNullOrWhiteSpace(request.Email))
            {
                _logger.LogWarning(
                    "Nao foi possivel provisionar UserProfile automaticamente porque o email nao foi informado. Subject={Subject}",
                    request.KeycloakSubject);
                return null;
            }

            var displayName = !string.IsNullOrWhiteSpace(request.DisplayName)
                ? request.DisplayName!
                : request.Email.Split('@')[0];

            var userProfile = new UserProfile(
                Guid.NewGuid(),
                KeycloakSubject.Create(request.KeycloakSubject),
                EmailAddress.Create(request.Email),
                PersonName.Create(displayName),
                _clock);

            await _userProfileRepository.AddAsync(userProfile, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            _logger.LogInformation(
                "UserProfile provisionado automaticamente para subject {Subject} e email {Email}. UserId={UserId}",
                request.KeycloakSubject,
                request.Email,
                userProfile.UserId);

            return userProfile.UserId;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Falha ao resolver OwnerUserId para o subject {Subject}", request.KeycloakSubject);
            return null;
        }
    }
}
