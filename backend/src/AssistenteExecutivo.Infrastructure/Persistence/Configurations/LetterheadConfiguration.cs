using AssistenteExecutivo.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AssistenteExecutivo.Infrastructure.Persistence.Configurations;

public class LetterheadConfiguration : IEntityTypeConfiguration<Letterhead>
{
    public void Configure(EntityTypeBuilder<Letterhead> builder)
    {
        builder.ToTable("Letterheads");

        builder.HasKey(l => l.LetterheadId);

        builder.Property(l => l.LetterheadId)
            .HasColumnName("LetterheadId")
            .IsRequired();

        builder.Property(l => l.OwnerUserId)
            .HasColumnName("OwnerUserId")
            .IsRequired();

        builder.Property(l => l.Name)
            .HasColumnName("Name")
            .HasMaxLength(200)
            .IsRequired();

        builder.Property(l => l.DesignData)
            .HasColumnName("DesignData")
            .HasColumnType("text") // TEXT no PostgreSQL para dados de design (JSON/XML)
            .IsRequired();

        builder.Property(l => l.IsActive)
            .HasColumnName("IsActive")
            .IsRequired()
            .HasDefaultValue(true);

        builder.Property(l => l.CreatedAt)
            .HasColumnName("CreatedAt")
            .IsRequired();

        builder.Property(l => l.UpdatedAt)
            .HasColumnName("UpdatedAt")
            .IsRequired();

        // Índices
        builder.HasIndex(l => l.OwnerUserId)
            .HasDatabaseName("IX_Letterheads_OwnerUserId");

        builder.HasIndex(l => l.IsActive)
            .HasDatabaseName("IX_Letterheads_IsActive");

        builder.HasIndex(l => new { l.OwnerUserId, l.IsActive })
            .HasDatabaseName("IX_Letterheads_OwnerUserId_IsActive");
    }
}

