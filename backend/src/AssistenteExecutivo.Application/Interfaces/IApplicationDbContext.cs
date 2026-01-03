using AssistenteExecutivo.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace AssistenteExecutivo.Application.Interfaces;

public interface IApplicationDbContext
{
    DbSet<UserProfile> UserProfiles { get; }
    DbSet<CreditPackage> CreditPackages { get; }
    DbSet<CommissionAgreement> CommissionAgreements { get; }
    DbSet<AgreementParty> AgreementParties { get; }
    DbSet<Milestone> Milestones { get; }
    DbSet<EscrowAccount> EscrowAccounts { get; }
    DbSet<EscrowTransaction> EscrowTransactions { get; }
    DbSet<NegotiationSession> NegotiationSessions { get; }
    DbSet<NegotiationProposal> NegotiationProposals { get; }
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}

