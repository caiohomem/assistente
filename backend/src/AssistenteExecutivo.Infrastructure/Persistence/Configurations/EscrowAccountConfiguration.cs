using AssistenteExecutivo.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AssistenteExecutivo.Infrastructure.Persistence.Configurations;

public class EscrowAccountConfiguration : IEntityTypeConfiguration<EscrowAccount>
{
    public void Configure(EntityTypeBuilder<EscrowAccount> builder)
    {
        builder.ToTable("EscrowAccounts");
        builder.HasKey(e => e.EscrowAccountId);

        builder.Property(e => e.AgreementId).IsRequired();
        builder.Property(e => e.OwnerUserId).IsRequired();
        builder.Property(e => e.Currency)
            .HasMaxLength(3)
            .IsRequired();

        builder.Property(e => e.Status)
            .HasConversion<string>()
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(e => e.StripeConnectedAccountId)
            .HasMaxLength(200);

        builder.Property(e => e.CreatedAt).IsRequired();
        builder.Property(e => e.UpdatedAt).IsRequired();

        builder.HasMany(e => e.Transactions)
            .WithOne()
            .HasForeignKey(t => t.EscrowAccountId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Navigation(e => e.Transactions).UsePropertyAccessMode(PropertyAccessMode.Field);
    }
}
