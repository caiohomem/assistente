using AssistenteExecutivo.Domain.DomainEvents;
using AssistenteExecutivo.Domain.Enums;
using AssistenteExecutivo.Domain.Exceptions;
using AssistenteExecutivo.Domain.Interfaces;
using AssistenteExecutivo.Domain.ValueObjects;

namespace AssistenteExecutivo.Domain.Entities;

public class UserProfile
{
    private readonly List<IDomainEvent> _domainEvents = new();

    private UserProfile() { } // EF Core

    public UserProfile(
        Guid userId,
        KeycloakSubject keycloakSubject,
        EmailAddress email,
        PersonName displayName,
        IClock clock)
    {
        if (userId == Guid.Empty)
            throw new DomainException("Domain:UserIdObrigatorio");

        if (keycloakSubject == null)
            throw new DomainException("Domain:KeycloakSubjectObrigatorio");

        if (email == null)
            throw new DomainException("Domain:EmailObrigatorio");

        if (displayName == null)
            throw new DomainException("Domain:DisplayNameObrigatorio");

        if (clock == null)
            throw new DomainException("Domain:ClockObrigatorio");

        UserId = userId;
        KeycloakSubject = keycloakSubject;
        Email = email;
        DisplayName = displayName;
        Status = UserStatus.Active;
        CreditBalance = 0;
        CreatedAt = clock.UtcNow;
    }

    public Guid UserId { get; private set; }
    public KeycloakSubject KeycloakSubject { get; private set; } = null!;
    public EmailAddress Email { get; private set; } = null!;
    public PersonName DisplayName { get; private set; } = null!;
    public UserStatus Status { get; private set; }
    public Guid? SubscriptionId { get; private set; }
    public int CreditBalance { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime? LastLoginAt { get; private set; }
    public string? PasswordResetToken { get; private set; }
    public DateTime? PasswordResetTokenExpiresAt { get; private set; }

    public IReadOnlyCollection<IDomainEvent> DomainEvents => _domainEvents.AsReadOnly();

    public void ProvisionFromKeycloak(
        KeycloakSubject keycloakSubject,
        EmailAddress email,
        PersonName displayName,
        IClock clock)
    {
        if (keycloakSubject == null)
            throw new DomainException("Domain:KeycloakSubjectObrigatorio");

        if (email == null)
            throw new DomainException("Domain:EmailObrigatorio");

        if (displayName == null)
            throw new DomainException("Domain:DisplayNameObrigatorio");

        // KeycloakSubject é imutável após vínculo inicial
        if (KeycloakSubject != null && KeycloakSubject != keycloakSubject)
            throw new DomainException("Domain:KeycloakSubjectImutavel");

        KeycloakSubject = keycloakSubject;
        Email = email;
        DisplayName = displayName;
        Status = UserStatus.Active;

        _domainEvents.Add(new UserProvisionedFromKeycloak(UserId, keycloakSubject, clock.UtcNow));
    }

    public void UpdateSubscription(Guid? subscriptionId)
    {
        SubscriptionId = subscriptionId;
    }

    public void UpdateCreditBalance(int creditBalance)
    {
        if (creditBalance < 0)
            throw new DomainException("Domain:CreditBalanceNaoPodeSerNegativo");

        CreditBalance = creditBalance;
    }

    public void ConsumeCredits(int amount)
    {
        if (amount <= 0)
            throw new DomainException("Domain:AmountDeveSerPositivo");

        if (CreditBalance < amount)
            throw new DomainException("Domain:SaldoInsuficiente");

        CreditBalance -= amount;
    }

    public void GrantCredits(int amount)
    {
        if (amount <= 0)
            throw new DomainException("Domain:AmountDeveSerPositivo");

        CreditBalance += amount;
    }

    public void UpdateProfile(PersonName? displayName = null)
    {
        if (Status == UserStatus.Deleted)
            throw new DomainException("Domain:UsuarioDeletadoNaoPodeSerAtualizado");

        if (displayName != null)
        {
            DisplayName = displayName;
        }
    }

    public void RecordLogin(LoginContext loginContext, IClock clock)
    {
        if (Status != UserStatus.Active)
            throw new DomainException("Domain:UsuarioNaoAtivo");

        LastLoginAt = clock.UtcNow;

        _domainEvents.Add(new UserLoggedIn(UserId, loginContext.AuthMethod, clock.UtcNow));
    }

    public void Suspend(string reason)
    {
        if (Status == UserStatus.Deleted)
            throw new DomainException("Domain:UsuarioDeletadoNaoPodeSerSuspenso");

        Status = UserStatus.Suspended;

        _domainEvents.Add(new UserSuspended(UserId, reason, DateTime.UtcNow));
    }

    public void Activate()
    {
        if (Status == UserStatus.Deleted)
            throw new DomainException("Domain:UsuarioDeletadoNaoPodeSerAtivado");

        Status = UserStatus.Active;
    }

    public void Delete()
    {
        Status = UserStatus.Deleted;
    }

    public void GeneratePasswordResetToken(IClock clock)
    {
        if (Status != UserStatus.Active)
            throw new DomainException("Domain:UsuarioNaoAtivo");

        PasswordResetToken = Guid.NewGuid().ToString("N");
        PasswordResetTokenExpiresAt = clock.UtcNow.AddHours(24);

        _domainEvents.Add(new PasswordResetRequested(
            UserId,
            Email.Value,
            PasswordResetToken,
            PasswordResetTokenExpiresAt.Value,
            clock.UtcNow));
    }

    public bool ValidatePasswordResetToken(string token, IClock clock)
    {
        if (string.IsNullOrWhiteSpace(PasswordResetToken))
            return false;

        if (PasswordResetToken != token)
            return false;

        if (!PasswordResetTokenExpiresAt.HasValue)
            return false;

        if (clock.UtcNow > PasswordResetTokenExpiresAt.Value)
            return false;

        return true;
    }

    public void InvalidatePasswordResetToken()
    {
        PasswordResetToken = null;
        PasswordResetTokenExpiresAt = null;
    }

    public void ClearDomainEvents()
    {
        _domainEvents.Clear();
    }
}

