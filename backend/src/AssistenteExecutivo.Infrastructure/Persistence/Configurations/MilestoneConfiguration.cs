using AssistenteExecutivo.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AssistenteExecutivo.Infrastructure.Persistence.Configurations;

public class MilestoneConfiguration : IEntityTypeConfiguration<Milestone>
{
    public void Configure(EntityTypeBuilder<Milestone> builder)
    {
        builder.ToTable("Milestones");
        builder.HasKey(m => m.MilestoneId);

        builder.Property(m => m.Description)
            .IsRequired()
            .HasMaxLength(500);

        builder.Property(m => m.AgreementId).IsRequired();
        builder.Property(m => m.DueDate).IsRequired();
        builder.Property(m => m.Status)
            .HasConversion<string>()
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(m => m.CompletionNotes)
            .HasMaxLength(2000);

        builder.OwnsOne(m => m.Value, money =>
        {
            money.Property(v => v.Amount)
                .HasColumnName("ValueAmount")
                .HasColumnType("decimal(18,2)");
            money.Property(v => v.Currency)
                .HasColumnName("ValueCurrency")
                .HasMaxLength(3)
                .IsRequired();
        });
    }
}
