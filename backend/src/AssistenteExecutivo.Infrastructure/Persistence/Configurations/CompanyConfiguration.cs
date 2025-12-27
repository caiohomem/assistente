using AssistenteExecutivo.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using System.Text.Json;

namespace AssistenteExecutivo.Infrastructure.Persistence.Configurations;

public class CompanyConfiguration : IEntityTypeConfiguration<Company>
{
    public void Configure(EntityTypeBuilder<Company> builder)
    {
        builder.ToTable("Companies");

        builder.HasKey(c => c.CompanyId);

        builder.Property(c => c.CompanyId)
            .HasColumnName("CompanyId")
            .IsRequired();

        builder.Property(c => c.Name)
            .HasColumnName("Name")
            .HasMaxLength(200)
            .IsRequired();

        // Collection: Domains (JSON)
        builder.Property(c => c.Domains)
            .HasColumnName("Domains")
            .HasConversion(
                v => JsonSerializer.Serialize(v, (JsonSerializerOptions?)null),
                v => JsonSerializer.Deserialize<List<string>>(v, (JsonSerializerOptions?)null) ?? new List<string>())
            .Metadata.SetValueComparer(
                new Microsoft.EntityFrameworkCore.ChangeTracking.ValueComparer<IReadOnlyCollection<string>>(
                    (c1, c2) => c1 != null && c2 != null && c1.SequenceEqual(c2),
                    c => c != null ? c.Aggregate(0, (a, v) => HashCode.Combine(a, v.GetHashCode())) : 0,
                    c => c != null ? c.ToList() : new List<string>()));

        builder.Property(c => c.Notes)
            .HasColumnName("Notes")
            .HasMaxLength(2000);

        builder.Property(c => c.CreatedAt)
            .HasColumnName("CreatedAt")
            .IsRequired();

        // Índice: Name (para busca por nome)
        builder.HasIndex(c => c.Name)
            .HasDatabaseName("IX_Companies_Name");
    }
}






