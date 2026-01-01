using AssistenteExecutivo.Domain.Entities;
using AssistenteExecutivo.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AssistenteExecutivo.Infrastructure.Persistence.Configurations;

public class WorkflowConfiguration : IEntityTypeConfiguration<Workflow>
{
    public void Configure(EntityTypeBuilder<Workflow> builder)
    {
        builder.ToTable("Workflows");

        builder.HasKey(w => w.WorkflowId);

        builder.Property(w => w.WorkflowId)
            .HasColumnName("WorkflowId")
            .IsRequired();

        builder.Property(w => w.OwnerUserId)
            .HasColumnName("OwnerUserId")
            .IsRequired();

        builder.Property(w => w.Name)
            .HasColumnName("Name")
            .HasMaxLength(200)
            .IsRequired();

        builder.Property(w => w.Description)
            .HasColumnName("Description")
            .HasMaxLength(2000);

        builder.Property(w => w.SpecJson)
            .HasColumnName("SpecJson")
            .HasColumnType("jsonb")
            .IsRequired();

        builder.Property(w => w.SpecVersion)
            .HasColumnName("SpecVersion")
            .IsRequired();

        builder.Property(w => w.N8nWorkflowId)
            .HasColumnName("N8nWorkflowId")
            .HasMaxLength(100);

        builder.Property(w => w.IdempotencyKey)
            .HasColumnName("IdempotencyKey")
            .HasMaxLength(500);

        builder.Property(w => w.Status)
            .HasColumnName("Status")
            .HasConversion<int>()
            .IsRequired();

        builder.Property(w => w.CreatedAt)
            .HasColumnName("CreatedAt")
            .IsRequired();

        builder.Property(w => w.UpdatedAt)
            .HasColumnName("UpdatedAt")
            .IsRequired();

        // Owned Type: WorkflowTrigger
        builder.OwnsOne(w => w.Trigger, trigger =>
        {
            trigger.Property(t => t.Type)
                .HasColumnName("TriggerType")
                .HasConversion<int>()
                .IsRequired();

            trigger.Property(t => t.CronExpression)
                .HasColumnName("TriggerCronExpression")
                .HasMaxLength(100);

            trigger.Property(t => t.EventName)
                .HasColumnName("TriggerEventName")
                .HasMaxLength(200);

            trigger.Property(t => t.ConfigJson)
                .HasColumnName("TriggerConfigJson")
                .HasColumnType("jsonb");
        });

        builder.Navigation(w => w.Trigger).IsRequired();

        // Ignore domain events
        builder.Ignore(w => w.DomainEvents);

        // Indexes
        builder.HasIndex(w => w.OwnerUserId)
            .HasDatabaseName("IX_Workflows_OwnerUserId");

        builder.HasIndex(w => w.Status)
            .HasDatabaseName("IX_Workflows_Status");

        builder.HasIndex(w => new { w.OwnerUserId, w.Status })
            .HasDatabaseName("IX_Workflows_OwnerUserId_Status");

        builder.HasIndex(w => new { w.OwnerUserId, w.Name })
            .IsUnique()
            .HasDatabaseName("IX_Workflows_OwnerUserId_Name");

        builder.HasIndex(w => w.N8nWorkflowId)
            .HasDatabaseName("IX_Workflows_N8nWorkflowId");

        builder.HasIndex(w => w.IdempotencyKey)
            .IsUnique()
            .HasFilter("\"IdempotencyKey\" IS NOT NULL")
            .HasDatabaseName("IX_Workflows_IdempotencyKey");
    }
}
