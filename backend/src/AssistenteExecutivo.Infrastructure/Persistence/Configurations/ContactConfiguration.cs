using AssistenteExecutivo.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AssistenteExecutivo.Infrastructure.Persistence.Configurations;

public class ContactConfiguration : IEntityTypeConfiguration<Contact>
{
    public void Configure(EntityTypeBuilder<Contact> builder)
    {
        builder.ToTable("Contacts");

        builder.HasKey(c => c.ContactId);

        builder.Property(c => c.ContactId)
            .HasColumnName("ContactId")
            .IsRequired();

        builder.Property(c => c.OwnerUserId)
            .HasColumnName("OwnerUserId")
            .IsRequired();

        // Owned Type: PersonName
        builder.OwnsOne(c => c.Name, name =>
        {
            name.Property(n => n.FirstName)
                .HasColumnName("FirstName")
                .HasMaxLength(100)
                .IsRequired();

            name.Property(n => n.LastName)
                .HasColumnName("LastName")
                .HasMaxLength(100);

            name.Ignore(n => n.FullName);
        });

        builder.Property(c => c.JobTitle)
            .HasColumnName("JobTitle")
            .HasMaxLength(200);

        builder.Property(c => c.Company)
            .HasColumnName("Company")
            .HasMaxLength(200);

        // Owned Type: Address
        builder.OwnsOne(c => c.Address, address =>
        {
            address.Property(a => a.Street)
                .HasColumnName("AddressStreet")
                .HasMaxLength(200);

            address.Property(a => a.City)
                .HasColumnName("AddressCity")
                .HasMaxLength(100);

            address.Property(a => a.State)
                .HasColumnName("AddressState")
                .HasMaxLength(100);

            address.Property(a => a.ZipCode)
                .HasColumnName("AddressZipCode")
                .HasMaxLength(20);

            address.Property(a => a.Country)
                .HasColumnName("AddressCountry")
                .HasMaxLength(100);

            // Marcar como sempre criado para evitar problemas com optional dependent
            address.ToTable("Contacts");
        });

        // Mark Address navigation as required to always create instance
        builder.Navigation(c => c.Address).IsRequired();

        // Owned Collection: Emails
        builder.OwnsMany(c => c.Emails, email =>
        {
            email.ToTable("ContactEmails");
            email.WithOwner().HasForeignKey("ContactId");
            email.HasKey("ContactId", "Value");

            email.Property(e => e.Value)
                .HasColumnName("Email")
                .HasMaxLength(255)
                .IsRequired();
        });

        // Owned Collection: Phones
        builder.OwnsMany(c => c.Phones, phone =>
        {
            phone.ToTable("ContactPhones");
            phone.WithOwner().HasForeignKey("ContactId");
            phone.HasKey("ContactId", "Number");

            phone.Property(p => p.Number)
                .HasColumnName("Number")
                .HasMaxLength(20)
                .IsRequired();

            phone.Property(p => p.FormattedNumber)
                .HasColumnName("FormattedNumber")
                .HasMaxLength(30)
                .IsRequired();
        });

        // Owned Collection: Tags
        builder.OwnsMany(c => c.Tags, tag =>
        {
            tag.ToTable("ContactTags");
            tag.WithOwner().HasForeignKey("ContactId");
            tag.HasKey("ContactId", "Value");

            tag.Property(t => t.Value)
                .HasColumnName("Tag")
                .HasMaxLength(50)
                .IsRequired();
        });

        // Navigation: Relationships (one-to-many)
        builder.HasMany(c => c.Relationships)
            .WithOne()
            .HasForeignKey("SourceContactId")
            .OnDelete(DeleteBehavior.Restrict);

        builder.Property(c => c.CreatedAt)
            .HasColumnName("CreatedAt")
            .IsRequired();

        builder.Property(c => c.UpdatedAt)
            .HasColumnName("UpdatedAt")
            .IsRequired();

        builder.Property(c => c.IsDeleted)
            .HasColumnName("IsDeleted")
            .IsRequired()
            .HasDefaultValue(false);

        // Índices
        builder.HasIndex(c => c.OwnerUserId)
            .HasDatabaseName("IX_Contacts_OwnerUserId");

        builder.HasIndex(c => c.CreatedAt)
            .HasDatabaseName("IX_Contacts_CreatedAt");

        builder.HasIndex(c => c.IsDeleted)
            .HasDatabaseName("IX_Contacts_IsDeleted");

        // Índice composto para queries comuns
        builder.HasIndex(c => new { c.OwnerUserId, c.IsDeleted })
            .HasDatabaseName("IX_Contacts_OwnerUserId_IsDeleted");
    }
}

