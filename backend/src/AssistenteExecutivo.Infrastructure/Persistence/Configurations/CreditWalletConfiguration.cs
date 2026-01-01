using AssistenteExecutivo.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AssistenteExecutivo.Infrastructure.Persistence.Configurations;

public class CreditWalletConfiguration : IEntityTypeConfiguration<CreditWallet>
{
    public void Configure(EntityTypeBuilder<CreditWallet> builder)
    {
        builder.ToTable("CreditWallets");

        builder.HasKey(c => c.OwnerUserId);

        builder.Property(c => c.OwnerUserId)
            .HasColumnName("OwnerUserId")
            .IsRequired();

        builder.Property(c => c.CreatedAt)
            .HasColumnName("CreatedAt")
            .IsRequired();

        // Foreign Key para UserProfiles
        builder.HasOne<UserProfile>()
            .WithOne()
            .HasForeignKey<CreditWallet>(c => c.OwnerUserId)
            .OnDelete(DeleteBehavior.Restrict);

        // Collection: Transactions (one-to-many para CreditTransaction)
        builder.HasMany(c => c.Transactions)
            .WithOne()
            .HasForeignKey(t => t.OwnerUserId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Navigation(c => c.Transactions)
            .UsePropertyAccessMode(PropertyAccessMode.Field);

        // Índice: OwnerUserId já é PK, então não precisa de índice adicional
    }
}












