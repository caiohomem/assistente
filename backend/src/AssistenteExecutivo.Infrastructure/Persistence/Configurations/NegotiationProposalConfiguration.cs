using AssistenteExecutivo.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AssistenteExecutivo.Infrastructure.Persistence.Configurations;

public class NegotiationProposalConfiguration : IEntityTypeConfiguration<NegotiationProposal>
{
    public void Configure(EntityTypeBuilder<NegotiationProposal> builder)
    {
        builder.ToTable("NegotiationProposals");
        builder.HasKey(p => p.ProposalId);

        builder.Property(p => p.SessionId).IsRequired();
        builder.Property(p => p.PartyId);
        builder.Property(p => p.Source)
            .HasConversion<string>()
            .HasMaxLength(50)
            .IsRequired();
        builder.Property(p => p.Status)
            .HasConversion<string>()
            .HasMaxLength(50)
            .IsRequired();
        builder.Property(p => p.Content)
            .IsRequired();
        builder.Property(p => p.RejectionReason)
            .HasMaxLength(1000);
        builder.Property(p => p.CreatedAt).IsRequired();
        builder.Property(p => p.RespondedAt);
    }
}
