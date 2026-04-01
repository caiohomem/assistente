using AssistenteExecutivo.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AssistenteExecutivo.Infrastructure.Persistence.Configurations;

public class EscrowTransactionConfiguration : IEntityTypeConfiguration<EscrowTransaction>
{
    public void Configure(EntityTypeBuilder<EscrowTransaction> builder)
    {
        builder.ToTable("EscrowTransactions");
        builder.HasKey(t => t.TransactionId);

        builder.Property(t => t.EscrowAccountId).IsRequired();
        builder.Property(t => t.PartyId);
        builder.Property(t => t.Type)
            .HasConversion<string>()
            .HasMaxLength(50)
            .IsRequired();
        builder.Property(t => t.Status)
            .HasConversion<string>()
            .HasMaxLength(50)
            .IsRequired();
        builder.Property(t => t.ApprovalType)
            .HasConversion<string>()
            .HasMaxLength(50);

        builder.Property(t => t.Description).HasMaxLength(1000);
        builder.Property(t => t.RejectionReason).HasMaxLength(1000);
        builder.Property(t => t.DisputeReason).HasMaxLength(1000);
        builder.Property(t => t.StripePaymentIntentId).HasMaxLength(200);
        builder.Property(t => t.StripeTransferId).HasMaxLength(200);
        builder.Property(t => t.IdempotencyKey).HasMaxLength(200);
        builder.Property(t => t.CreatedAt).IsRequired();
        builder.Property(t => t.UpdatedAt).IsRequired();

        builder.OwnsOne(t => t.Amount, money =>
        {
            money.Property(v => v.Amount)
                .HasColumnName("Amount")
                .HasColumnType("decimal(18,2)");
            money.Property(v => v.Currency)
                .HasColumnName("Currency")
                .HasMaxLength(3)
                .IsRequired();
        });
    }
}
