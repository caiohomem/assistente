using AssistenteExecutivo.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AssistenteExecutivo.Infrastructure.Persistence.Configurations;

public class NegotiationSessionConfiguration : IEntityTypeConfiguration<NegotiationSession>
{
    public void Configure(EntityTypeBuilder<NegotiationSession> builder)
    {
        builder.ToTable("NegotiationSessions");
        builder.HasKey(s => s.SessionId);

        builder.Property(s => s.OwnerUserId).IsRequired();
        builder.Property(s => s.Title)
            .IsRequired()
            .HasMaxLength(200);
        builder.Property(s => s.Context)
            .HasMaxLength(4000);
        builder.Property(s => s.Status)
            .HasConversion<string>()
            .HasMaxLength(50)
            .IsRequired();
        builder.Property(s => s.GeneratedAgreementId);
        builder.Property(s => s.CreatedAt).IsRequired();
        builder.Property(s => s.UpdatedAt).IsRequired();
        builder.Property(s => s.LastAiSuggestionRequestedAt);

        builder.HasMany(s => s.Proposals)
            .WithOne()
            .HasForeignKey(p => p.SessionId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Navigation(s => s.Proposals).UsePropertyAccessMode(PropertyAccessMode.Field);
    }
}
