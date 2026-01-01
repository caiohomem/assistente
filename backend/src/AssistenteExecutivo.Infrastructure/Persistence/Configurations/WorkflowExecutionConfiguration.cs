using AssistenteExecutivo.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AssistenteExecutivo.Infrastructure.Persistence.Configurations;

public class WorkflowExecutionConfiguration : IEntityTypeConfiguration<WorkflowExecution>
{
    public void Configure(EntityTypeBuilder<WorkflowExecution> builder)
    {
        builder.ToTable("WorkflowExecutions");

        builder.HasKey(e => e.ExecutionId);

        builder.Property(e => e.ExecutionId)
            .HasColumnName("ExecutionId")
            .IsRequired();

        builder.Property(e => e.WorkflowId)
            .HasColumnName("WorkflowId")
            .IsRequired();

        builder.Property(e => e.OwnerUserId)
            .HasColumnName("OwnerUserId")
            .IsRequired();

        builder.Property(e => e.SpecVersionUsed)
            .HasColumnName("SpecVersionUsed")
            .IsRequired();

        builder.Property(e => e.InputJson)
            .HasColumnName("InputJson")
            .HasColumnType("jsonb");

        builder.Property(e => e.OutputJson)
            .HasColumnName("OutputJson")
            .HasColumnType("jsonb");

        builder.Property(e => e.Status)
            .HasColumnName("Status")
            .HasConversion<int>()
            .IsRequired();

        builder.Property(e => e.N8nExecutionId)
            .HasColumnName("N8nExecutionId")
            .HasMaxLength(100);

        builder.Property(e => e.ErrorMessage)
            .HasColumnName("ErrorMessage")
            .HasMaxLength(4000);

        builder.Property(e => e.CurrentStepIndex)
            .HasColumnName("CurrentStepIndex");

        builder.Property(e => e.IdempotencyKey)
            .HasColumnName("IdempotencyKey")
            .HasMaxLength(500);

        builder.Property(e => e.StartedAt)
            .HasColumnName("StartedAt")
            .IsRequired();

        builder.Property(e => e.CompletedAt)
            .HasColumnName("CompletedAt");

        // Ignore domain events
        builder.Ignore(e => e.DomainEvents);

        // Indexes
        builder.HasIndex(e => e.WorkflowId)
            .HasDatabaseName("IX_WorkflowExecutions_WorkflowId");

        builder.HasIndex(e => e.OwnerUserId)
            .HasDatabaseName("IX_WorkflowExecutions_OwnerUserId");

        builder.HasIndex(e => e.Status)
            .HasDatabaseName("IX_WorkflowExecutions_Status");

        builder.HasIndex(e => new { e.OwnerUserId, e.Status })
            .HasDatabaseName("IX_WorkflowExecutions_OwnerUserId_Status");

        builder.HasIndex(e => e.N8nExecutionId)
            .HasDatabaseName("IX_WorkflowExecutions_N8nExecutionId");

        builder.HasIndex(e => e.StartedAt)
            .HasDatabaseName("IX_WorkflowExecutions_StartedAt");

        builder.HasIndex(e => e.IdempotencyKey)
            .IsUnique()
            .HasFilter("\"IdempotencyKey\" IS NOT NULL")
            .HasDatabaseName("IX_WorkflowExecutions_IdempotencyKey");
    }
}
