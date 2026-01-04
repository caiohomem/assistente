using AssistenteExecutivo.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AssistenteExecutivo.Infrastructure.Persistence.Configurations;

public class AgreementPartyConfiguration : IEntityTypeConfiguration<AgreementParty>
{
    public void Configure(EntityTypeBuilder<AgreementParty> builder)
    {
        builder.ToTable("AgreementParties");
        builder.HasKey(p => p.PartyId);

        builder.Property(p => p.AgreementId)
            .IsRequired();

        builder.Property(p => p.PartyName)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(p => p.Email)
            .HasMaxLength(255);

        builder.Property(p => p.Role)
            .HasConversion<string>()
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(p => p.ContactId);
        builder.Property(p => p.CompanyId);
        builder.Property(p => p.StripeAccountId)
            .HasMaxLength(255);
        builder.Property(p => p.HasAccepted).IsRequired();
        builder.Property(p => p.AcceptedAt);
        builder.Property(p => p.CreatedAt).IsRequired();

        builder.OwnsOne(p => p.SplitPercentage, percentage =>
        {
            percentage.Property(v => v.Value)
                .HasColumnName("SplitPercentage")
                .HasColumnType("decimal(5,2)")
                .IsRequired();
        });
    }
}
