using AssistenteExecutivo.Domain.Entities;
using AssistenteExecutivo.Domain.ValueObjects;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using System.Text.Json;

namespace AssistenteExecutivo.Infrastructure.Persistence.Configurations;

public class MediaAssetConfiguration : IEntityTypeConfiguration<MediaAsset>
{
    public void Configure(EntityTypeBuilder<MediaAsset> builder)
    {
        builder.ToTable("MediaAssets");

        builder.HasKey(e => e.MediaId);

        builder.Property(e => e.MediaId)
            .IsRequired();

        builder.Property(e => e.OwnerUserId)
            .IsRequired();

        builder.Property(e => e.Kind)
            .HasConversion<int>()
            .IsRequired();

        builder.Property(e => e.CreatedAt)
            .IsRequired();

        // Owned Type: MediaRef
        builder.OwnsOne(e => e.MediaRef, mediaRef =>
        {
            mediaRef.Property(m => m.StorageKey)
                .HasColumnName("StorageKey")
                .IsRequired()
                .HasMaxLength(500);

            mediaRef.Property(m => m.Hash)
                .HasColumnName("Hash")
                .IsRequired()
                .HasMaxLength(128);

            mediaRef.Property(m => m.MimeType)
                .HasColumnName("MimeType")
                .IsRequired()
                .HasMaxLength(100);

            mediaRef.Property(m => m.SizeBytes)
                .HasColumnName("SizeBytes")
                .IsRequired();
        });

        // Metadata como JSON (TEXT no PostgreSQL)
        builder.Property(e => e.Metadata)
            .HasColumnType("text")
            .HasConversion(
                v => v == null || v.Count == 0 
                    ? "{}" 
                    : JsonSerializer.Serialize(v),
                v => string.IsNullOrWhiteSpace(v) || v == "{}"
                    ? new Dictionary<string, string>()
                    : JsonSerializer.Deserialize<Dictionary<string, string>>(v) ?? new Dictionary<string, string>()
            )
            .Metadata.SetValueComparer(
                new Microsoft.EntityFrameworkCore.ChangeTracking.ValueComparer<Dictionary<string, string>>(
                    (c1, c2) => c1 != null && c2 != null && c1.SequenceEqual(c2),
                    c => c != null 
                        ? c.Aggregate(0, (a, v) => HashCode.Combine(a, v.Key.GetHashCode(), v.Value.GetHashCode()))
                        : 0,
                    c => c != null ? new Dictionary<string, string>(c) : new Dictionary<string, string>()
                )
            );

        // Índices
        builder.HasIndex(e => e.OwnerUserId)
            .HasDatabaseName("IX_MediaAssets_OwnerUserId");

        builder.HasIndex(e => e.Kind)
            .HasDatabaseName("IX_MediaAssets_Kind");

        builder.HasIndex(e => e.CreatedAt)
            .HasDatabaseName("IX_MediaAssets_CreatedAt");

        // FileContent: armazena o conteúdo do arquivo no banco de dados (BYTEA no PostgreSQL)
        builder.Property(e => e.FileContent)
            .HasColumnName("FileContent")
            .HasColumnType("bytea")
            .IsRequired(false);
    }
}


