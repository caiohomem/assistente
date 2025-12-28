using AssistenteExecutivo.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AssistenteExecutivo.Infrastructure.Persistence.Configurations;

public class PlanConfiguration : IEntityTypeConfiguration<Plan>
{
    public void Configure(EntityTypeBuilder<Plan> builder)
    {
        builder.ToTable("Plans");

        builder.HasKey(p => p.PlanId);

        builder.Property(p => p.PlanId)
            .HasColumnName("PlanId")
            .IsRequired();

        builder.Property(p => p.Name)
            .HasColumnName("Name")
            .HasMaxLength(200)
            .IsRequired();

        builder.Property(p => p.Price)
            .HasColumnName("Price")
            .HasColumnType("decimal(18,2)")
            .IsRequired();

        builder.Property(p => p.Currency)
            .HasColumnName("Currency")
            .HasMaxLength(3)
            .IsRequired();

        builder.Property(p => p.Interval)
            .HasColumnName("Interval")
            .HasConversion<int>()
            .IsRequired();

        // Owned Type: PlanLimits
        builder.OwnsOne(p => p.Limits, limits =>
        {
            limits.Property(l => l.Contacts)
                .HasColumnName("LimitsContacts");

            limits.Property(l => l.Notes)
                .HasColumnName("LimitsNotes");

            limits.Property(l => l.CreditsPerMonth)
                .HasColumnName("LimitsCreditsPerMonth");

            limits.Property(l => l.StorageGB)
                .HasColumnName("LimitsStorageGB")
                .HasColumnType("decimal(18,2)");
        });

        // Mark Limits navigation as required to always create instance
        builder.Navigation(p => p.Limits).IsRequired();

        // Collection: Features (stored as delimited string for simplicity)
        // Mapear o campo privado _features usando backing field
        builder.Property<List<string>>("_features")
            .HasColumnName("Features")
            .HasConversion(
                v => v == null || v.Count == 0 ? string.Empty : string.Join(";", v),
                v => string.IsNullOrWhiteSpace(v)
                    ? new List<string>()
                    : v.Split(';', StringSplitOptions.RemoveEmptyEntries).ToList(),
                new ValueComparer<List<string>>(
                    (c1, c2) => c1 != null && c2 != null && c1.SequenceEqual(c2),
                    c => c.Aggregate(0, (a, v) => HashCode.Combine(a, v.GetHashCode())),
                    c => c.ToList()))
            .HasMaxLength(2000);

        builder.Property(p => p.IsActive)
            .HasColumnName("IsActive")
            .IsRequired()
            .HasDefaultValue(true);

        builder.Property(p => p.Highlighted)
            .HasColumnName("Highlighted")
            .IsRequired()
            .HasDefaultValue(false);

        builder.Property(p => p.CreatedAt)
            .HasColumnName("CreatedAt")
            .IsRequired();

        builder.Property(p => p.UpdatedAt)
            .HasColumnName("UpdatedAt")
            .IsRequired();

        // Índices
        builder.HasIndex(p => p.IsActive)
            .HasDatabaseName("IX_Plans_IsActive");

        builder.HasIndex(p => new { p.IsActive, p.Interval })
            .HasDatabaseName("IX_Plans_IsActive_Interval");
    }
}

