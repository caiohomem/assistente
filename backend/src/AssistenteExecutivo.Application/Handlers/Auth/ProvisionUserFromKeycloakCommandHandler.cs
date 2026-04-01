using AssistenteExecutivo.Application.Commands.Auth;
using AssistenteExecutivo.Application.Interfaces;
using AssistenteExecutivo.Domain.Constants;
using AssistenteExecutivo.Domain.Entities;
using AssistenteExecutivo.Domain.Enums;
using AssistenteExecutivo.Domain.Exceptions;
using AssistenteExecutivo.Domain.Interfaces;
using AssistenteExecutivo.Domain.ValueObjects;
using MediatR;
using Microsoft.Extensions.Logging;

namespace AssistenteExecutivo.Application.Handlers.Auth;

public class ProvisionUserFromKeycloakCommandHandler : IRequestHandler<ProvisionUserFromKeycloakCommand, ProvisionUserFromKeycloakResult>
{
    private readonly IUserProfileRepository _userProfileRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IClock _clock;
    private readonly ILogger<ProvisionUserFromKeycloakCommandHandler> _logger;
    private readonly IRelationshipTypeRepository _relationshipTypeRepository;

    public ProvisionUserFromKeycloakCommandHandler(
        IUserProfileRepository userProfileRepository,
        IUnitOfWork unitOfWork,
        IClock clock,
        ILogger<ProvisionUserFromKeycloakCommandHandler> logger,
        IRelationshipTypeRepository relationshipTypeRepository)
    {
        _userProfileRepository = userProfileRepository;
        _unitOfWork = unitOfWork;
        _clock = clock;
        _logger = logger;
        _relationshipTypeRepository = relationshipTypeRepository;
    }

    public async Task<ProvisionUserFromKeycloakResult> Handle(ProvisionUserFromKeycloakCommand request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.KeycloakSubject))
            throw new ArgumentException("KeycloakSubject é obrigatório", nameof(request));

        if (string.IsNullOrWhiteSpace(request.Email))
            throw new ArgumentException("Email é obrigatório", nameof(request));

        var firstName = request.FirstName;
        var lastName = request.LastName;

        if (string.IsNullOrWhiteSpace(firstName) && !string.IsNullOrWhiteSpace(request.FullName))
        {
            var nameParts = request.FullName.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            firstName = nameParts.FirstOrDefault() ?? "Usuário";
            lastName = string.Join(" ", nameParts.Skip(1));
        }

        if (string.IsNullOrWhiteSpace(firstName))
        {
            firstName = "Usuário";
        }

        var keycloakSubject = KeycloakSubject.Create(request.KeycloakSubject);
        var email = EmailAddress.Create(request.Email);
        var displayName = PersonName.Create(firstName, lastName ?? string.Empty);

        var existingUser = await _userProfileRepository.GetByEmailAsync(request.Email, cancellationToken);

        if (existingUser == null)
        {
            _logger.LogInformation("Criando UserProfile para {Email} (KeycloakSubject={KeycloakSubject})", email.Value, keycloakSubject.Value);

            var userId = Guid.NewGuid();
            var userProfile = new UserProfile(
                userId: userId,
                keycloakSubject: keycloakSubject,
                email: email,
                displayName: displayName,
                clock: _clock);

            await _userProfileRepository.AddAsync(userProfile, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            await EnsureDefaultRelationshipTypesAsync(userId, cancellationToken);

            _logger.LogInformation(
                "UserProfile criado com sucesso para {Email} (UserId={UserId}, KeycloakSubject={KeycloakSubject})",
                email.Value,
                userId,
                keycloakSubject.Value);

            return new ProvisionUserFromKeycloakResult
            {
                UserId = userId,
                WasCreated = true,
                Email = email.Value
            };
        }

        if (existingUser.Status == UserStatus.Deleted)
        {
            _logger.LogWarning("Login bloqueado: usuário deletado tentou autenticar. Email={Email}, UserId={UserId}", email.Value, existingUser.UserId);
            throw new DomainException("Domain:UsuarioDeletado");
        }

        if (existingUser.Status == UserStatus.Suspended)
        {
            _logger.LogWarning("Login bloqueado: usuário suspenso tentou autenticar. Email={Email}, UserId={UserId}", email.Value, existingUser.UserId);
            throw new DomainException("Domain:UsuarioSuspenso");
        }

        if (existingUser.KeycloakSubject.Value != keycloakSubject.Value)
        {
            _logger.LogWarning(
                "UserProfile para {Email} já existe com outro KeycloakSubject ({ExistingSubject}). Não será alterado para {NewSubject}.",
                email.Value,
                existingUser.KeycloakSubject.Value,
                keycloakSubject.Value);

            return new ProvisionUserFromKeycloakResult
            {
                UserId = existingUser.UserId,
                WasCreated = false,
                Email = email.Value
            };
        }

        existingUser.ProvisionFromKeycloak(keycloakSubject, email, displayName, _clock);
        await _userProfileRepository.UpdateAsync(existingUser, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        _logger.LogInformation(
            "UserProfile atualizado para {Email} (UserId={UserId}, KeycloakSubject={KeycloakSubject})",
            email.Value,
            existingUser.UserId,
            keycloakSubject.Value);

        return new ProvisionUserFromKeycloakResult
        {
            UserId = existingUser.UserId,
            WasCreated = false,
            Email = email.Value
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

