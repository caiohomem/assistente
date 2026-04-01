using AssistenteExecutivo.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AssistenteExecutivo.Infrastructure.Persistence.Configurations;

public class RelationshipTypeConfiguration : IEntityTypeConfiguration<RelationshipType>
{
    public void Configure(EntityTypeBuilder<RelationshipType> builder)
    {
        builder.ToTable("RelationshipTypes");

        builder.HasKey(rt => rt.RelationshipTypeId);

        builder.Property(rt => rt.RelationshipTypeId)
            .HasColumnName("RelationshipTypeId")
            .IsRequired();

        builder.Property(rt => rt.OwnerUserId)
            .HasColumnName("OwnerUserId")
            .IsRequired();

        builder.Property(rt => rt.Name)
            .HasColumnName("Name")
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(rt => rt.IsDefault)
            .HasColumnName("IsDefault")
            .IsRequired()
            .HasDefaultValue(false);

        builder.Property(rt => rt.CreatedAt)
            .HasColumnName("CreatedAt")
            .IsRequired();

        builder.Property(rt => rt.UpdatedAt)
            .HasColumnName("UpdatedAt")
            .IsRequired();

        builder.HasIndex(rt => new { rt.OwnerUserId, rt.Name })
            .IsUnique()
            .HasDatabaseName("UQ_RelationshipTypes_Owner_Name");

        builder.HasOne<UserProfile>()
            .WithMany()
            .HasForeignKey(rt => rt.OwnerUserId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
