using AssistenteExecutivo.Application.Interfaces;
using AssistenteExecutivo.Domain.Entities;
using AssistenteExecutivo.Domain.ValueObjects;
using AssistenteExecutivo.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace AssistenteExecutivo.Infrastructure.Repositories;

public class UserProfileRepository : IUserProfileRepository
{
    private readonly ApplicationDbContext _context;

    public UserProfileRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<UserProfile?> GetByIdAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        return await _context.UserProfiles
            .FirstOrDefaultAsync(u => u.UserId == userId, cancellationToken);
    }

    public async Task<UserProfile?> GetByEmailAsync(string email, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(email))
            return null;

        var normalizedEmail = EmailAddress.Create(email).Value;

        return await _context.UserProfiles
            .FirstOrDefaultAsync(u => u.Email.Value == normalizedEmail, cancellationToken);
    }

    public async Task<UserProfile?> GetByKeycloakSubjectAsync(string keycloakSubject, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(keycloakSubject))
            return null;

        var normalizedSubject = KeycloakSubject.Create(keycloakSubject).Value;

        return await _context.UserProfiles
            .FirstOrDefaultAsync(u => u.KeycloakSubject.Value == normalizedSubject, cancellationToken);
    }

    public async Task<UserProfile?> GetByKeycloakSubjectOrEmailAsync(string keycloakSubject, string email, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(keycloakSubject) && string.IsNullOrWhiteSpace(email))
            return null;

        var query = _context.UserProfiles.AsQueryable();

        if (!string.IsNullOrWhiteSpace(keycloakSubject))
        {
            var normalizedSubject = KeycloakSubject.Create(keycloakSubject).Value;
            query = query.Where(u => u.KeycloakSubject.Value == normalizedSubject);
        }

        if (!string.IsNullOrWhiteSpace(email))
        {
            var normalizedEmail = EmailAddress.Create(email).Value;
            query = query.Where(u => u.Email.Value == normalizedEmail);
        }

        return await query.FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<bool> ExistsByEmailAsync(string email, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(email))
            return false;

        var normalizedEmail = EmailAddress.Create(email).Value;

        return await _context.UserProfiles
            .AnyAsync(u => u.Email.Value == normalizedEmail, cancellationToken);
    }

    public async Task<bool> ExistsByKeycloakSubjectAsync(string keycloakSubject, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(keycloakSubject))
            return false;

        var normalizedSubject = KeycloakSubject.Create(keycloakSubject).Value;

        return await _context.UserProfiles
            .AnyAsync(u => u.KeycloakSubject.Value == normalizedSubject, cancellationToken);
    }

    public async Task AddAsync(UserProfile userProfile, CancellationToken cancellationToken = default)
    {
        await _context.UserProfiles.AddAsync(userProfile, cancellationToken);
    }

    public async Task UpdateAsync(UserProfile userProfile, CancellationToken cancellationToken = default)
    {
        var entry = _context.Entry(userProfile);

        if (entry.State == EntityState.Detached)
        {
            _context.UserProfiles.Update(userProfile);
        }

        await Task.CompletedTask;
    }
}






