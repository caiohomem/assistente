using AssistenteExecutivo.Domain.DomainEvents;
using AssistenteExecutivo.Domain.Entities;
using AssistenteExecutivo.Domain.Enums;
using AssistenteExecutivo.Domain.Exceptions;
using AssistenteExecutivo.Domain.Interfaces;
using AssistenteExecutivo.Domain.ValueObjects;
using FluentAssertions;

namespace AssistenteExecutivo.Domain.Tests.Entities;

public class UserProfileTests
{
    private readonly IClock _clock;

    public UserProfileTests()
    {
        _clock = new TestClock();
    }

    [Fact]
    public void Constructor_ValidData_ShouldCreate()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var keycloakSubject = KeycloakSubject.Create("sub-123");
        var email = EmailAddress.Create("test@example.com");
        var displayName = PersonName.Create("João", "Silva");

        // Act
        var user = new UserProfile(userId, keycloakSubject, email, displayName, _clock);

        // Assert
        user.UserId.Should().Be(userId);
        user.KeycloakSubject.Should().Be(keycloakSubject);
        user.Email.Should().Be(email);
        user.DisplayName.Should().Be(displayName);
        user.Status.Should().Be(UserStatus.Active);
        user.DomainEvents.Should().BeEmpty();
    }

    [Fact]
    public void ProvisionFromKeycloak_ShouldUpdateAndEmitEvent()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var keycloakSubject = KeycloakSubject.Create("sub-123");
        var email = EmailAddress.Create("test@example.com");
        var displayName = PersonName.Create("João", "Silva");
        var user = new UserProfile(userId, keycloakSubject, email, displayName, _clock);
        user.ClearDomainEvents();

        var newEmail = EmailAddress.Create("new@example.com");
        var newDisplayName = PersonName.Create("Maria", "Santos");

        // Act
        user.ProvisionFromKeycloak(keycloakSubject, newEmail, newDisplayName, _clock);

        // Assert
        user.Email.Should().Be(newEmail);
        user.DisplayName.Should().Be(newDisplayName);
        user.DomainEvents.Should().ContainSingle()
            .Which.Should().BeOfType<UserProvisionedFromKeycloak>();
    }

    [Fact]
    public void ProvisionFromKeycloak_DifferentKeycloakSubject_ShouldThrow()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var keycloakSubject1 = KeycloakSubject.Create("sub-123");
        var email = EmailAddress.Create("test@example.com");
        var displayName = PersonName.Create("João", "Silva");
        var user = new UserProfile(userId, keycloakSubject1, email, displayName, _clock);

        var keycloakSubject2 = KeycloakSubject.Create("sub-456");

        // Act & Assert
        var act = () => user.ProvisionFromKeycloak(keycloakSubject2, email, displayName, _clock);
        act.Should().Throw<DomainException>()
            .WithMessage("*KeycloakSubjectImutavel*");
    }

    [Fact]
    public void RecordLogin_ShouldUpdateLastLoginAndEmitEvent()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var keycloakSubject = KeycloakSubject.Create("sub-123");
        var email = EmailAddress.Create("test@example.com");
        var displayName = PersonName.Create("João", "Silva");
        var user = new UserProfile(userId, keycloakSubject, email, displayName, _clock);
        user.ClearDomainEvents();

        var loginContext = new LoginContext("192.168.1.1", "Mozilla/5.0", AuthMethod.OAuth, "corr-123");

        // Act
        user.RecordLogin(loginContext, _clock);

        // Assert
        user.LastLoginAt.Should().NotBeNull();
        user.DomainEvents.Should().ContainSingle()
            .Which.Should().BeOfType<UserLoggedIn>();
    }

    [Fact]
    public void Suspend_ShouldChangeStatusAndEmitEvent()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var keycloakSubject = KeycloakSubject.Create("sub-123");
        var email = EmailAddress.Create("test@example.com");
        var displayName = PersonName.Create("João", "Silva");
        var user = new UserProfile(userId, keycloakSubject, email, displayName, _clock);
        user.ClearDomainEvents();

        // Act
        user.Suspend("Violação de termos");

        // Assert
        user.Status.Should().Be(UserStatus.Suspended);
        user.DomainEvents.Should().ContainSingle()
            .Which.Should().BeOfType<UserSuspended>();
    }

    [Fact]
    public void Delete_ShouldChangeStatusToDeleted()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var keycloakSubject = KeycloakSubject.Create("sub-123");
        var email = EmailAddress.Create("test@example.com");
        var displayName = PersonName.Create("João", "Silva");
        var user = new UserProfile(userId, keycloakSubject, email, displayName, _clock);

        // Act
        user.Delete();

        // Assert
        user.Status.Should().Be(UserStatus.Deleted);
    }

    [Fact]
    public void UpdateSubscription_ShouldUpdateSubscriptionId()
    {
        // Arrange
        var user = CreateUser();
        var subscriptionId = Guid.NewGuid();

        // Act
        user.UpdateSubscription(subscriptionId);

        // Assert
        user.SubscriptionId.Should().Be(subscriptionId);
    }

    [Fact]
    public void UpdateCreditBalance_ShouldUpdateBalance()
    {
        // Arrange
        var user = CreateUser();

        // Act
        user.UpdateCreditBalance(100);

        // Assert
        user.CreditBalance.Should().Be(100);
    }

    [Fact]
    public void UpdateCreditBalance_NegativeBalance_ShouldThrow()
    {
        // Arrange
        var user = CreateUser();

        // Act & Assert
        var act = () => user.UpdateCreditBalance(-10);
        act.Should().Throw<DomainException>()
            .WithMessage("*CreditBalanceNaoPodeSerNegativo*");
    }

    [Fact]
    public void ConsumeCredits_WithSufficientBalance_ShouldDeduct()
    {
        // Arrange
        var user = CreateUser();
        user.UpdateCreditBalance(100);

        // Act
        user.ConsumeCredits(30);

        // Assert
        user.CreditBalance.Should().Be(70);
    }

    [Fact]
    public void ConsumeCredits_InsufficientBalance_ShouldThrow()
    {
        // Arrange
        var user = CreateUser();
        user.UpdateCreditBalance(20);

        // Act & Assert
        var act = () => user.ConsumeCredits(30);
        act.Should().Throw<DomainException>()
            .WithMessage("*SaldoInsuficiente*");
    }

    [Fact]
    public void GrantCredits_ShouldAddCredits()
    {
        // Arrange
        var user = CreateUser();
        user.UpdateCreditBalance(50);

        // Act
        user.GrantCredits(30);

        // Assert
        user.CreditBalance.Should().Be(80);
    }

    [Fact]
    public void ConsumeCredits_ZeroAmount_ShouldThrow()
    {
        // Arrange
        var user = CreateUser();
        user.UpdateCreditBalance(100);

        // Act & Assert
        var act = () => user.ConsumeCredits(0);
        act.Should().Throw<DomainException>()
            .WithMessage("*AmountDeveSerPositivo*");
    }

    [Fact]
    public void GrantCredits_ZeroAmount_ShouldThrow()
    {
        // Arrange
        var user = CreateUser();

        // Act & Assert
        var act = () => user.GrantCredits(0);
        act.Should().Throw<DomainException>()
            .WithMessage("*AmountDeveSerPositivo*");
    }

    private UserProfile CreateUser()
    {
        return CreateUserWithClock(_clock);
    }

    private UserProfile CreateUserWithClock(IClock clock)
    {
        var userId = Guid.NewGuid();
        var keycloakSubject = KeycloakSubject.Create("sub-123");
        var email = EmailAddress.Create("test@example.com");
        var displayName = PersonName.Create("João", "Silva");
        return new UserProfile(userId, keycloakSubject, email, displayName, clock);
    }

    [Fact]
    public void UpdateProfile_DeletedUser_ShouldThrow()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var keycloakSubject = KeycloakSubject.Create("sub-123");
        var email = EmailAddress.Create("test@example.com");
        var displayName = PersonName.Create("João", "Silva");
        var user = new UserProfile(userId, keycloakSubject, email, displayName, _clock);
        user.Delete();

        var newDisplayName = PersonName.Create("Maria", "Santos");

        // Act & Assert
        var act = () => user.UpdateProfile(newDisplayName);
        act.Should().Throw<DomainException>()
            .WithMessage("*UsuarioDeletadoNaoPodeSerAtualizado*");
    }

    [Fact]
    public void UpdateProfile_WithDisplayName_ShouldUpdate()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var keycloakSubject = KeycloakSubject.Create("sub-123");
        var email = EmailAddress.Create("test@example.com");
        var displayName = PersonName.Create("João", "Silva");
        var user = new UserProfile(userId, keycloakSubject, email, displayName, _clock);
        var newDisplayName = PersonName.Create("Maria", "Santos");

        // Act
        user.UpdateProfile(newDisplayName);

        // Assert
        user.DisplayName.Should().Be(newDisplayName);
    }

    [Fact]
    public void UpdateProfile_WithNullDisplayName_ShouldNotUpdate()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var keycloakSubject = KeycloakSubject.Create("sub-123");
        var email = EmailAddress.Create("test@example.com");
        var displayName = PersonName.Create("João", "Silva");
        var user = new UserProfile(userId, keycloakSubject, email, displayName, _clock);

        // Act
        user.UpdateProfile(null);

        // Assert
        user.DisplayName.Should().Be(displayName);
    }

    [Fact]
    public void RecordLogin_UserNotActive_ShouldThrow()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var keycloakSubject = KeycloakSubject.Create("sub-123");
        var email = EmailAddress.Create("test@example.com");
        var displayName = PersonName.Create("João", "Silva");
        var user = new UserProfile(userId, keycloakSubject, email, displayName, _clock);
        user.Suspend("Test");
        var loginContext = new LoginContext("192.168.1.1", "Mozilla/5.0", AuthMethod.OAuth);

        // Act & Assert
        var act = () => user.RecordLogin(loginContext, _clock);
        act.Should().Throw<DomainException>()
            .WithMessage("*UsuarioNaoAtivo*");
    }

    [Fact]
    public void Suspend_DeletedUser_ShouldThrow()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var keycloakSubject = KeycloakSubject.Create("sub-123");
        var email = EmailAddress.Create("test@example.com");
        var displayName = PersonName.Create("João", "Silva");
        var user = new UserProfile(userId, keycloakSubject, email, displayName, _clock);
        user.Delete();

        // Act & Assert
        var act = () => user.Suspend("Test");
        act.Should().Throw<DomainException>()
            .WithMessage("*UsuarioDeletadoNaoPodeSerSuspenso*");
    }

    [Fact]
    public void Activate_ShouldChangeStatusToActive()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var keycloakSubject = KeycloakSubject.Create("sub-123");
        var email = EmailAddress.Create("test@example.com");
        var displayName = PersonName.Create("João", "Silva");
        var user = new UserProfile(userId, keycloakSubject, email, displayName, _clock);
        user.Suspend("Test");

        // Act
        user.Activate();

        // Assert
        user.Status.Should().Be(UserStatus.Active);
    }

    [Fact]
    public void Activate_DeletedUser_ShouldThrow()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var keycloakSubject = KeycloakSubject.Create("sub-123");
        var email = EmailAddress.Create("test@example.com");
        var displayName = PersonName.Create("João", "Silva");
        var user = new UserProfile(userId, keycloakSubject, email, displayName, _clock);
        user.Delete();

        // Act & Assert
        var act = () => user.Activate();
        act.Should().Throw<DomainException>()
            .WithMessage("*UsuarioDeletadoNaoPodeSerAtivado*");
    }

    [Fact]
    public void ProvisionFromKeycloak_NullKeycloakSubject_ShouldThrow()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var keycloakSubject = KeycloakSubject.Create("sub-123");
        var email = EmailAddress.Create("test@example.com");
        var displayName = PersonName.Create("João", "Silva");
        var user = new UserProfile(userId, keycloakSubject, email, displayName, _clock);

        // Act & Assert
        var act = () => user.ProvisionFromKeycloak(null!, email, displayName, _clock);
        act.Should().Throw<DomainException>()
            .WithMessage("*KeycloakSubjectObrigatorio*");
    }

    [Fact]
    public void ProvisionFromKeycloak_NullEmail_ShouldThrow()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var keycloakSubject = KeycloakSubject.Create("sub-123");
        var email = EmailAddress.Create("test@example.com");
        var displayName = PersonName.Create("João", "Silva");
        var user = new UserProfile(userId, keycloakSubject, email, displayName, _clock);

        // Act & Assert
        var act = () => user.ProvisionFromKeycloak(keycloakSubject, null!, displayName, _clock);
        act.Should().Throw<DomainException>()
            .WithMessage("*EmailObrigatorio*");
    }

    [Fact]
    public void ProvisionFromKeycloak_NullDisplayName_ShouldThrow()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var keycloakSubject = KeycloakSubject.Create("sub-123");
        var email = EmailAddress.Create("test@example.com");
        var displayName = PersonName.Create("João", "Silva");
        var user = new UserProfile(userId, keycloakSubject, email, displayName, _clock);

        // Act & Assert
        var act = () => user.ProvisionFromKeycloak(keycloakSubject, email, null!, _clock);
        act.Should().Throw<DomainException>()
            .WithMessage("*DisplayNameObrigatorio*");
    }

    [Fact]
    public void Reactivate_DeletedUser_ShouldReactivate()
    {
        // Arrange
        var user = CreateUser();
        user.Delete();
        user.ClearDomainEvents();

        // Act
        user.Reactivate(_clock);

        // Assert
        user.Status.Should().Be(UserStatus.Active);
        user.LastLoginAt.Should().BeNull();
        user.PasswordResetToken.Should().BeNull();
        user.PasswordResetTokenExpiresAt.Should().BeNull();
        user.DomainEvents.Should().ContainSingle()
            .Which.Should().BeOfType<UserReactivated>();
    }

    [Fact]
    public void Reactivate_NotDeletedUser_ShouldThrow()
    {
        // Arrange
        var user = CreateUser();

        // Act & Assert
        var act = () => user.Reactivate(_clock);
        act.Should().Throw<DomainException>()
            .WithMessage("*ApenasUsuariosDeletadosPodemSerReativados*");
    }

    [Fact]
    public void ReactivateWithNewKeycloakSubject_DeletedUser_ShouldReactivate()
    {
        // Arrange
        var user = CreateUser();
        user.Delete();
        user.ClearDomainEvents();
        var newKeycloakSubject = KeycloakSubject.Create("sub-456");

        // Act
        user.ReactivateWithNewKeycloakSubject(newKeycloakSubject, _clock);

        // Assert
        user.Status.Should().Be(UserStatus.Active);
        user.KeycloakSubject.Should().Be(newKeycloakSubject);
        user.LastLoginAt.Should().BeNull();
        user.PasswordResetToken.Should().BeNull();
        user.PasswordResetTokenExpiresAt.Should().BeNull();
        user.DomainEvents.Should().ContainSingle()
            .Which.Should().BeOfType<UserReactivated>();
    }

    [Fact]
    public void ReactivateWithNewKeycloakSubject_NotDeletedUser_ShouldThrow()
    {
        // Arrange
        var user = CreateUser();
        var newKeycloakSubject = KeycloakSubject.Create("sub-456");

        // Act & Assert
        var act = () => user.ReactivateWithNewKeycloakSubject(newKeycloakSubject, _clock);
        act.Should().Throw<DomainException>()
            .WithMessage("*ApenasUsuariosDeletadosPodemSerReativados*");
    }

    [Fact]
    public void ReactivateWithNewKeycloakSubject_NullKeycloakSubject_ShouldThrow()
    {
        // Arrange
        var user = CreateUser();
        user.Delete();

        // Act & Assert
        var act = () => user.ReactivateWithNewKeycloakSubject(null!, _clock);
        act.Should().Throw<DomainException>()
            .WithMessage("*KeycloakSubjectObrigatorio*");
    }

    [Fact]
    public void GeneratePasswordResetToken_ActiveUser_ShouldGenerateToken()
    {
        // Arrange
        var user = CreateUser();
        user.ClearDomainEvents();

        // Act
        user.GeneratePasswordResetToken(_clock);

        // Assert
        user.PasswordResetToken.Should().NotBeNullOrEmpty();
        user.PasswordResetTokenExpiresAt.Should().NotBeNull();
        user.PasswordResetTokenExpiresAt!.Value.Should().BeCloseTo(_clock.UtcNow.AddHours(24), TimeSpan.FromMinutes(1));
        user.DomainEvents.Should().ContainSingle()
            .Which.Should().BeOfType<PasswordResetRequested>();
    }

    [Fact]
    public void GeneratePasswordResetToken_SuspendedUser_ShouldThrow()
    {
        // Arrange
        var user = CreateUser();
        user.Suspend("Test");

        // Act & Assert
        var act = () => user.GeneratePasswordResetToken(_clock);
        act.Should().Throw<DomainException>()
            .WithMessage("*UsuarioNaoAtivo*");
    }

    [Fact]
    public void ValidatePasswordResetToken_ValidToken_ShouldReturnTrue()
    {
        // Arrange
        var user = CreateUser();
        user.GeneratePasswordResetToken(_clock);
        var token = user.PasswordResetToken!;

        // Act
        var result = user.ValidatePasswordResetToken(token, _clock);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public void ValidatePasswordResetToken_InvalidToken_ShouldReturnFalse()
    {
        // Arrange
        var user = CreateUser();
        user.GeneratePasswordResetToken(_clock);

        // Act
        var result = user.ValidatePasswordResetToken("invalid-token", _clock);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void ValidatePasswordResetToken_ExpiredToken_ShouldReturnFalse()
    {
        // Arrange
        var pastClock = new PastClock();
        var user = CreateUserWithClock(pastClock);
        user.GeneratePasswordResetToken(pastClock);
        var token = user.PasswordResetToken!;
        
        // Use a clock that returns a time after expiration
        var futureClock = new FutureClock(pastClock.UtcNow.AddHours(25));

        // Act
        var result = user.ValidatePasswordResetToken(token, futureClock);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void ValidatePasswordResetToken_NoToken_ShouldReturnFalse()
    {
        // Arrange
        var user = CreateUser();

        // Act
        var result = user.ValidatePasswordResetToken("any-token", _clock);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void InvalidatePasswordResetToken_ShouldClearToken()
    {
        // Arrange
        var user = CreateUser();
        user.GeneratePasswordResetToken(_clock);

        // Act
        user.InvalidatePasswordResetToken();

        // Assert
        user.PasswordResetToken.Should().BeNull();
        user.PasswordResetTokenExpiresAt.Should().BeNull();
    }

    [Fact]
    public void UpdateSubscription_Null_ShouldSetToNull()
    {
        // Arrange
        var user = CreateUser();
        user.UpdateSubscription(Guid.NewGuid());

        // Act
        user.UpdateSubscription(null);

        // Assert
        user.SubscriptionId.Should().BeNull();
    }

    private class TestClock : IClock
    {
        public DateTime UtcNow => DateTime.UtcNow;
    }

    private class PastClock : IClock
    {
        public DateTime UtcNow => DateTime.UtcNow.AddDays(-1);
    }

    private class FutureClock : IClock
    {
        private readonly DateTime _futureTime;

        public FutureClock(DateTime futureTime)
        {
            _futureTime = futureTime;
        }

        public DateTime UtcNow => _futureTime;
    }
}

