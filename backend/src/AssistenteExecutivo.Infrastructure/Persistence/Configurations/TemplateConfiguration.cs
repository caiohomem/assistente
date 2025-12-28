using AssistenteExecutivo.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AssistenteExecutivo.Infrastructure.Persistence.Configurations;

public class TemplateConfiguration : IEntityTypeConfiguration<Template>
{
    public void Configure(EntityTypeBuilder<Template> builder)
    {
        builder.ToTable("Templates");

        builder.HasKey(t => t.TemplateId);

        builder.Property(t => t.TemplateId)
            .HasColumnName("TemplateId")
            .IsRequired();

        builder.Property(t => t.OwnerUserId)
            .HasColumnName("OwnerUserId")
            .IsRequired();

        builder.Property(t => t.Name)
            .HasColumnName("Name")
            .HasMaxLength(200)
            .IsRequired();

        builder.Property(t => t.Type)
            .HasColumnName("Type")
            .HasConversion<int>()
            .IsRequired();

        builder.Property(t => t.Body)
            .HasColumnName("Body")
            .HasColumnType("text") // TEXT no PostgreSQL para corpo do template
            .IsRequired();

        builder.Property(t => t.PlaceholdersSchema)
            .HasColumnName("PlaceholdersSchema")
            .HasColumnType("text"); // TEXT no PostgreSQL para schema de placeholders (JSON)

        builder.Property(t => t.Active)
            .HasColumnName("Active")
            .IsRequired()
            .HasDefaultValue(true);

        builder.Property(t => t.CreatedAt)
            .HasColumnName("CreatedAt")
            .IsRequired();

        builder.Property(t => t.UpdatedAt)
            .HasColumnName("UpdatedAt")
            .IsRequired();

        // Índices
        builder.HasIndex(t => t.OwnerUserId)
            .HasDatabaseName("IX_Templates_OwnerUserId");

        builder.HasIndex(t => t.Type)
            .HasDatabaseName("IX_Templates_Type");

        builder.HasIndex(t => t.Active)
            .HasDatabaseName("IX_Templates_Active");

        builder.HasIndex(t => new { t.OwnerUserId, t.Active })
            .HasDatabaseName("IX_Templates_OwnerUserId_Active");

        builder.HasIndex(t => new { t.OwnerUserId, t.Type, t.Active })
            .HasDatabaseName("IX_Templates_OwnerUserId_Type_Active");
    }
}

