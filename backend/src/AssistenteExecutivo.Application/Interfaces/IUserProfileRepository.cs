using AssistenteExecutivo.Domain.Entities;
using AssistenteExecutivo.Domain.ValueObjects;

namespace AssistenteExecutivo.Application.Interfaces;

public interface IUserProfileRepository
{
    Task<UserProfile?> GetByIdAsync(Guid userId, CancellationToken cancellationToken = default);
    Task<UserProfile?> GetByEmailAsync(string email, CancellationToken cancellationToken = default);
    Task<UserProfile?> GetByKeycloakSubjectAsync(string keycloakSubject, CancellationToken cancellationToken = default);
    Task<UserProfile?> GetByKeycloakSubjectOrEmailAsync(string keycloakSubject, string email, CancellationToken cancellationToken = default);
    Task<bool> ExistsByEmailAsync(string email, CancellationToken cancellationToken = default);
    Task<bool> ExistsByKeycloakSubjectAsync(string keycloakSubject, CancellationToken cancellationToken = default);
    Task AddAsync(UserProfile userProfile, CancellationToken cancellationToken = default);
    Task UpdateAsync(UserProfile userProfile, CancellationToken cancellationToken = default);
}


