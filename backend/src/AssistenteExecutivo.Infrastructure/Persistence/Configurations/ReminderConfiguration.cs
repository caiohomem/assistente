using AssistenteExecutivo.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AssistenteExecutivo.Infrastructure.Persistence.Configurations;

public class ReminderConfiguration : IEntityTypeConfiguration<Reminder>
{
    public void Configure(EntityTypeBuilder<Reminder> builder)
    {
        builder.ToTable("Reminders");

        builder.HasKey(r => r.ReminderId);

        builder.Property(r => r.ReminderId)
            .HasColumnName("ReminderId")
            .IsRequired();

        builder.Property(r => r.OwnerUserId)
            .HasColumnName("OwnerUserId")
            .IsRequired();

        builder.Property(r => r.ContactId)
            .HasColumnName("ContactId")
            .IsRequired();

        builder.Property(r => r.Reason)
            .HasColumnName("Reason")
            .HasMaxLength(2000)
            .IsRequired();

        builder.Property(r => r.SuggestedMessage)
            .HasColumnName("SuggestedMessage")
            .HasMaxLength(2000);

        builder.Property(r => r.ScheduledFor)
            .HasColumnName("ScheduledFor")
            .IsRequired();

        builder.Property(r => r.Status)
            .HasColumnName("Status")
            .HasConversion<int>()
            .IsRequired();

        builder.Property(r => r.CreatedAt)
            .HasColumnName("CreatedAt")
            .IsRequired();

        builder.Property(r => r.UpdatedAt)
            .HasColumnName("UpdatedAt")
            .IsRequired();

        // Índices
        builder.HasIndex(r => r.OwnerUserId)
            .HasDatabaseName("IX_Reminders_OwnerUserId");

        builder.HasIndex(r => r.ContactId)
            .HasDatabaseName("IX_Reminders_ContactId");

        builder.HasIndex(r => r.Status)
            .HasDatabaseName("IX_Reminders_Status");

        builder.HasIndex(r => r.ScheduledFor)
            .HasDatabaseName("IX_Reminders_ScheduledFor");

        builder.HasIndex(r => new { r.OwnerUserId, r.Status })
            .HasDatabaseName("IX_Reminders_OwnerUserId_Status");

        builder.HasIndex(r => new { r.Status, r.ScheduledFor })
            .HasDatabaseName("IX_Reminders_Status_ScheduledFor");
    }
}

