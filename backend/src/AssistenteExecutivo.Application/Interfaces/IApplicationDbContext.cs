using AssistenteExecutivo.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace AssistenteExecutivo.Application.Interfaces;

public interface IApplicationDbContext
{
    DbSet<UserProfile> UserProfiles { get; }
    DbSet<CreditPackage> CreditPackages { get; }
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}


