using AssistenteExecutivo.Domain.Entities;
using AssistenteExecutivo.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AssistenteExecutivo.Infrastructure.Persistence.Configurations;

public class DraftDocumentConfiguration : IEntityTypeConfiguration<DraftDocument>
{
    public void Configure(EntityTypeBuilder<DraftDocument> builder)
    {
        builder.ToTable("DraftDocuments");

        builder.HasKey(d => d.DraftId);

        builder.Property(d => d.DraftId)
            .HasColumnName("DraftId")
            .IsRequired();

        builder.Property(d => d.OwnerUserId)
            .HasColumnName("OwnerUserId")
            .IsRequired();

        builder.Property(d => d.ContactId)
            .HasColumnName("ContactId");

        builder.Property(d => d.CompanyId)
            .HasColumnName("CompanyId");

        builder.Property(d => d.DocumentType)
            .HasColumnName("DocumentType")
            .HasConversion<int>()
            .IsRequired();

        builder.Property(d => d.TemplateId)
            .HasColumnName("TemplateId");

        builder.Property(d => d.LetterheadId)
            .HasColumnName("LetterheadId");

        builder.Property(d => d.Content)
            .HasColumnName("Content")
            .HasColumnType("text") // TEXT no PostgreSQL para conteúdo de documentos
            .IsRequired();

        builder.Property(d => d.Status)
            .HasColumnName("Status")
            .HasConversion<int>()
            .IsRequired();

        builder.Property(d => d.CreatedAt)
            .HasColumnName("CreatedAt")
            .IsRequired();

        builder.Property(d => d.UpdatedAt)
            .HasColumnName("UpdatedAt")
            .IsRequired();

        // Índices
        builder.HasIndex(d => d.OwnerUserId)
            .HasDatabaseName("IX_DraftDocuments_OwnerUserId");

        builder.HasIndex(d => d.ContactId)
            .HasDatabaseName("IX_DraftDocuments_ContactId");

        builder.HasIndex(d => d.CompanyId)
            .HasDatabaseName("IX_DraftDocuments_CompanyId");

        builder.HasIndex(d => d.DocumentType)
            .HasDatabaseName("IX_DraftDocuments_DocumentType");

        builder.HasIndex(d => d.Status)
            .HasDatabaseName("IX_DraftDocuments_Status");

        builder.HasIndex(d => new { d.OwnerUserId, d.Status })
            .HasDatabaseName("IX_DraftDocuments_OwnerUserId_Status");
    }
}

