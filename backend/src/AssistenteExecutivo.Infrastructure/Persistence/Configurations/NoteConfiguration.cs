using AssistenteExecutivo.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AssistenteExecutivo.Infrastructure.Persistence.Configurations;

public class NoteConfiguration : IEntityTypeConfiguration<Note>
{
    public void Configure(EntityTypeBuilder<Note> builder)
    {
        builder.ToTable("Notes");

        builder.HasKey(e => e.NoteId);

        builder.Property(e => e.NoteId)
            .IsRequired();

        builder.Property(e => e.ContactId)
            .IsRequired();

        builder.Property(e => e.AuthorId)
            .IsRequired();

        builder.Property(e => e.Type)
            .HasConversion<int>()
            .IsRequired();

        builder.Property(e => e.RawContent)
            .IsRequired()
            .HasMaxLength(4000);

        // StructuredData como JSON (TEXT no PostgreSQL)
        builder.Property(e => e.StructuredData)
            .HasColumnType("text")
            .HasConversion(
                v => v ?? null,
                v => v ?? null
            );

        builder.Property(e => e.Version)
            .IsRequired();

        builder.Property(e => e.CreatedAt)
            .IsRequired();

        builder.Property(e => e.UpdatedAt)
            .IsRequired();

        // Foreign Key para Contacts
        builder.HasOne<Contact>()
            .WithMany()
            .HasForeignKey(e => e.ContactId)
            .OnDelete(DeleteBehavior.Restrict);

        // Índices
        builder.HasIndex(e => e.ContactId)
            .HasDatabaseName("IX_Notes_ContactId");

        builder.HasIndex(e => e.AuthorId)
            .HasDatabaseName("IX_Notes_AuthorId");

        builder.HasIndex(e => e.CreatedAt)
            .HasDatabaseName("IX_Notes_CreatedAt");

        builder.HasIndex(e => e.Type)
            .HasDatabaseName("IX_Notes_Type");
    }
}


