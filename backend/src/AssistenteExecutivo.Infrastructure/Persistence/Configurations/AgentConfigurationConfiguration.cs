using AssistenteExecutivo.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AssistenteExecutivo.Infrastructure.Persistence.Configurations;

public class AgentConfigurationConfiguration : IEntityTypeConfiguration<AgentConfiguration>
{
    public void Configure(EntityTypeBuilder<AgentConfiguration> builder)
    {
        builder.ToTable("AgentConfigurations");

        builder.HasKey(c => c.ConfigurationId);

        builder.Property(c => c.ConfigurationId)
            .HasColumnName("ConfigurationId")
            .IsRequired();

        builder.Property(c => c.OcrPrompt)
            .HasColumnName("OcrPrompt")
            .IsRequired();

        builder.Property(c => c.TranscriptionPrompt)
            .HasColumnName("TranscriptionPrompt");

        builder.Property(c => c.WorkflowPrompt)
            .HasColumnName("WorkflowPrompt");

        builder.Property(c => c.CreatedAt)
            .HasColumnName("CreatedAt")
            .IsRequired();

        builder.Property(c => c.UpdatedAt)
            .HasColumnName("UpdatedAt")
            .IsRequired();

        // Índice único para garantir que só existe uma configuração
        builder.HasIndex(c => c.ConfigurationId)
            .IsUnique()
            .HasDatabaseName("IX_AgentConfigurations_ConfigurationId");
    }
}




