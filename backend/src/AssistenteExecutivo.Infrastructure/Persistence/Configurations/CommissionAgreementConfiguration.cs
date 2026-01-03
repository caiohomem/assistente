using AssistenteExecutivo.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AssistenteExecutivo.Infrastructure.Persistence.Configurations;

public class CommissionAgreementConfiguration : IEntityTypeConfiguration<CommissionAgreement>
{
    public void Configure(EntityTypeBuilder<CommissionAgreement> builder)
    {
        builder.ToTable("CommissionAgreements");
        builder.HasKey(a => a.AgreementId);

        builder.Property(a => a.Title)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(a => a.Description)
            .HasMaxLength(2000);

        builder.Property(a => a.Terms)
            .HasMaxLength(4000);

        builder.OwnsOne(a => a.TotalValue, money =>
        {
            money.Property(m => m.Amount)
                .HasColumnName("TotalValueAmount")
                .HasColumnType("decimal(18,2)");
            money.Property(m => m.Currency)
                .HasColumnName("TotalValueCurrency")
                .HasMaxLength(3)
                .IsRequired();
        });

        builder.Property(a => a.Status)
            .HasConversion<string>()
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(a => a.OwnerUserId).IsRequired();
        builder.Property(a => a.EscrowAccountId);
        builder.Property(a => a.CreatedAt).IsRequired();
        builder.Property(a => a.UpdatedAt).IsRequired();

        builder.HasMany(a => a.Parties)
            .WithOne()
            .HasForeignKey(p => p.AgreementId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(a => a.Milestones)
            .WithOne()
            .HasForeignKey(m => m.AgreementId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Navigation(a => a.Parties).UsePropertyAccessMode(PropertyAccessMode.Field);
        builder.Navigation(a => a.Milestones).UsePropertyAccessMode(PropertyAccessMode.Field);
    }
}
