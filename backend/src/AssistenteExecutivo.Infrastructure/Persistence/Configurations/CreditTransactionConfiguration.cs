using AssistenteExecutivo.Domain.Entities;
using AssistenteExecutivo.Domain.ValueObjects;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AssistenteExecutivo.Infrastructure.Persistence.Configurations;

public class CreditTransactionConfiguration : IEntityTypeConfiguration<CreditTransaction>
{
    public void Configure(EntityTypeBuilder<CreditTransaction> builder)
    {
        builder.ToTable("CreditTransactions");

        builder.HasKey(c => c.TransactionId);

        builder.Property(c => c.TransactionId)
            .HasColumnName("TransactionId")
            .IsRequired();

        builder.Property(c => c.OwnerUserId)
            .HasColumnName("OwnerUserId")
            .IsRequired();

        builder.Property(c => c.Type)
            .HasColumnName("Type")
            .HasConversion<int>()
            .IsRequired();

        // Owned Type: CreditAmount
        builder.OwnsOne(c => c.Amount, amount =>
        {
            amount.Property(a => a.Value)
                .HasColumnName("Amount")
                .HasColumnType("decimal(18,2)")
                .IsRequired();
        });

        builder.Property(c => c.Reason)
            .HasColumnName("Reason")
            .HasMaxLength(500);

        builder.Property(c => c.OccurredAt)
            .HasColumnName("OccurredAt")
            .IsRequired();

        // Owned Type: IdempotencyKey (nullable)
        builder.Property(c => c.IdempotencyKey)
            .HasColumnName("IdempotencyKey")
            .HasMaxLength(255)
            .HasConversion(
                v => v == null ? null : v.Value,
                v => string.IsNullOrWhiteSpace(v) ? null : IdempotencyKey.Create(v))
            .Metadata.SetValueComparer(
                new Microsoft.EntityFrameworkCore.ChangeTracking.ValueComparer<IdempotencyKey?>(
                    (k1, k2) => Equals(k1, k2),
                    k => k == null ? 0 : k.Value.GetHashCode(),
                    k => k == null ? null : IdempotencyKey.Create(k.Value)
                )
            );

        // Shadow property para criar índice em propriedade de Owned Type nullable
        // Mapeia para a mesma coluna que o Owned Type já usa


        // Índices
        builder.HasIndex(c => c.OwnerUserId)
            .HasDatabaseName("IX_CreditTransactions_OwnerUserId");

        builder.HasIndex(c => c.Type)
            .HasDatabaseName("IX_CreditTransactions_Type");

        builder.HasIndex(c => c.OccurredAt)
            .HasDatabaseName("IX_CreditTransactions_OccurredAt");

        // Índice único em IdempotencyKey (quando não null)
        // Nota: O EF Core traduz automaticamente o filtro para a sintaxe correta do banco
        // Para PostgreSQL, usa aspas duplas; para SQL Server, usa colchetes
        builder.HasIndex(c => c.IdempotencyKey)
            .HasDatabaseName("IX_CreditTransactions_IdempotencyKey")
            .IsUnique()
            .HasFilter("\"IdempotencyKey\" IS NOT NULL");
    }
}
