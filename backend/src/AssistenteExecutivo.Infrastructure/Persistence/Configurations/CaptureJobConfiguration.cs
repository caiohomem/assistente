using AssistenteExecutivo.Domain.Entities;
using AssistenteExecutivo.Domain.ValueObjects;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using System.Text.Json;

namespace AssistenteExecutivo.Infrastructure.Persistence.Configurations;

public class CaptureJobConfiguration : IEntityTypeConfiguration<CaptureJob>
{
    public void Configure(EntityTypeBuilder<CaptureJob> builder)
    {
        builder.ToTable("CaptureJobs");

        builder.HasKey(e => e.JobId);

        builder.Property(e => e.JobId)
            .IsRequired();

        builder.Property(e => e.OwnerUserId)
            .IsRequired();

        builder.Property(e => e.Type)
            .HasConversion<int>()
            .IsRequired();

        builder.Property(e => e.MediaId)
            .IsRequired();

        builder.Property(e => e.ContactId)
            .IsRequired(false);

        builder.Property(e => e.Status)
            .HasConversion<int>()
            .IsRequired();

        builder.Property(e => e.RequestedAt)
            .IsRequired();

        builder.Property(e => e.CompletedAt)
            .IsRequired(false);

        builder.Property(e => e.ErrorCode)
            .HasMaxLength(100)
            .IsRequired(false);

        builder.Property(e => e.ErrorMessage)
            .HasMaxLength(2000)
            .IsRequired(false);

        builder.Property(e => e.AudioSummary)
            .HasMaxLength(4000)
            .IsRequired(false);

        // Foreign Keys
        builder.HasOne<UserProfile>()
            .WithMany()
            .HasForeignKey(e => e.OwnerUserId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<Contact>()
            .WithMany()
            .HasForeignKey(e => e.ContactId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<MediaAsset>()
            .WithMany()
            .HasForeignKey(e => e.MediaId)
            .OnDelete(DeleteBehavior.Restrict);

        // Owned Type: OcrExtract (CardScanResult)
        builder.OwnsOne(e => e.CardScanResult, ocrExtract =>
        {
            ocrExtract.Property(o => o.RawText)
                .HasColumnName("CardScanResult_RawText")
                .HasColumnType("text")
                .IsRequired(false);

            ocrExtract.Property(o => o.Name)
                .HasColumnName("CardScanResult_Name")
                .HasMaxLength(200)
                .IsRequired(false);

            ocrExtract.Property(o => o.Email)
                .HasColumnName("CardScanResult_Email")
                .HasMaxLength(255)
                .IsRequired(false);

            ocrExtract.Property(o => o.Phone)
                .HasColumnName("CardScanResult_Phone")
                .HasMaxLength(50)
                .IsRequired(false);

            ocrExtract.Property(o => o.Company)
                .HasColumnName("CardScanResult_Company")
                .HasMaxLength(200)
                .IsRequired(false);

            ocrExtract.Property(o => o.JobTitle)
                .HasColumnName("CardScanResult_JobTitle")
                .HasMaxLength(200)
                .IsRequired(false);

            ocrExtract.Property(o => o.AiRawResponse)
                .HasColumnName("CardScanResult_AiRawResponse")
                .HasColumnType("text")
                .IsRequired(false);

            // ConfidenceScores como JSON
            ocrExtract.Property(o => o.ConfidenceScores)
                .HasColumnName("CardScanResult_ConfidenceScores")
                .HasColumnType("text")
                .HasConversion(
                    v => JsonSerializer.Serialize(v ?? new Dictionary<string, decimal>()),
                    v => string.IsNullOrWhiteSpace(v)
                        ? new Dictionary<string, decimal>()
                        : JsonSerializer.Deserialize<Dictionary<string, decimal>>(v) ?? new Dictionary<string, decimal>()
                )
                .Metadata.SetValueComparer(
                    new Microsoft.EntityFrameworkCore.ChangeTracking.ValueComparer<Dictionary<string, decimal>>(
                        (c1, c2) => c1 != null && c2 != null && c1.SequenceEqual(c2),
                        c => c != null
                            ? c.Aggregate(0, (a, v) => HashCode.Combine(a, v.Key.GetHashCode(), v.Value.GetHashCode()))
                            : 0,
                        c => c != null ? new Dictionary<string, decimal>(c) : new Dictionary<string, decimal>()
                    )
                );
        });

        // Owned Type: Transcript (AudioTranscript)
        // Como Transcript é um ValueObject com propriedades readonly e construtor parametrizado,
        // armazenamos tudo como JSON usando conversão direta
        builder.Property(e => e.AudioTranscript)
            .HasConversion(
                v => v == null
                    ? null
                    : JsonSerializer.Serialize(new { Text = v.Text, Segments = v.Segments }),
                v => string.IsNullOrWhiteSpace(v)
                    ? null
                    : DeserializeTranscript(v)
            )
            .HasColumnType("text")
            .HasColumnName("AudioTranscript_Json")
            .IsRequired(false);

        // Collection: ExtractedTasks (owned)
        builder.OwnsMany(e => e.ExtractedTasks, task =>
        {
            task.ToTable("CaptureJobExtractedTasks");

            task.WithOwner()
                .HasForeignKey("JobId");

            task.Property<int>("Id")
                .ValueGeneratedOnAdd();

            task.HasKey("Id");

            task.Property(t => t.Description)
                .HasColumnName("Description")
                .IsRequired()
                .HasColumnType("text"); // TEXT no PostgreSQL (ilimitado até 1GB)

            task.Property(t => t.DueDate)
                .HasColumnName("DueDate")
                .IsRequired(false);

            task.Property(t => t.Priority)
                .HasColumnName("Priority")
                .HasMaxLength(50)
                .IsRequired(false);
        });

        // Índices
        builder.HasIndex(e => e.OwnerUserId)
            .HasDatabaseName("IX_CaptureJobs_OwnerUserId");

        builder.HasIndex(e => e.Status)
            .HasDatabaseName("IX_CaptureJobs_Status");

        builder.HasIndex(e => e.Type)
            .HasDatabaseName("IX_CaptureJobs_Type");

        builder.HasIndex(e => e.RequestedAt)
            .HasDatabaseName("IX_CaptureJobs_RequestedAt");

        builder.HasIndex(e => e.ContactId)
            .HasDatabaseName("IX_CaptureJobs_ContactId");
    }

    // Classe auxiliar para deserialização do Transcript
    private class TranscriptJson
    {
        public string? Text { get; set; }
        public List<TranscriptSegment>? Segments { get; set; }
    }

    // Método auxiliar para deserialização (fora da expression tree)
    private static Transcript? DeserializeTranscript(string json)
    {
        var jsonObj = JsonSerializer.Deserialize<TranscriptJson>(json);
        return jsonObj == null
            ? null
            : new Transcript(jsonObj.Text ?? string.Empty, jsonObj.Segments);
    }
}
