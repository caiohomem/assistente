using AssistenteExecutivo.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AssistenteExecutivo.Infrastructure.Persistence.Configurations;

public class RelationshipConfiguration : IEntityTypeConfiguration<Relationship>
{
    public void Configure(EntityTypeBuilder<Relationship> builder)
    {
        builder.ToTable("Relationships");

        builder.HasKey(r => r.RelationshipId);

        builder.Property(r => r.RelationshipId)
            .HasColumnName("RelationshipId")
            .IsRequired();

        // Foreign Key: SourceContactId
        // Relacionamento configurado em ContactConfiguration via Contact.Relationships
        builder.Property(r => r.SourceContactId)
            .HasColumnName("SourceContactId")
            .IsRequired();

        // Foreign Key: TargetContactId
        builder.Property(r => r.TargetContactId)
            .HasColumnName("TargetContactId")
            .IsRequired();

        builder.HasOne<Contact>()
            .WithMany()
            .HasForeignKey(r => r.TargetContactId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Property(r => r.Type)
            .HasColumnName("Type")
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(r => r.Description)
            .HasColumnName("Description")
            .HasMaxLength(500);

        builder.Property(r => r.Strength)
            .HasColumnName("Strength")
            .HasColumnType("real")
            .IsRequired();

        builder.Property(r => r.IsConfirmed)
            .HasColumnName("IsConfirmed")
            .IsRequired()
            .HasDefaultValue(false);

        // Índices
        builder.HasIndex(r => r.SourceContactId)
            .HasDatabaseName("IX_Relationships_SourceContactId");

        builder.HasIndex(r => r.TargetContactId)
            .HasDatabaseName("IX_Relationships_TargetContactId");

        builder.HasIndex(r => r.Type)
            .HasDatabaseName("IX_Relationships_Type");

        // Unique constraint: SourceContactId + TargetContactId (evita relacionamentos duplicados)
        builder.HasIndex(r => new { r.SourceContactId, r.TargetContactId })
            .IsUnique()
            .HasDatabaseName("UQ_Relationships_SourceContactId_TargetContactId");
    }
}

