using AssistenteExecutivo.Application.Interfaces;
using AssistenteExecutivo.Domain.Entities;
using AssistenteExecutivo.Domain.Notifications;
using AssistenteExecutivo.Infrastructure.Persistence.Configurations;
using Microsoft.EntityFrameworkCore;

namespace AssistenteExecutivo.Infrastructure.Persistence;

public class ApplicationDbContext : DbContext, IApplicationDbContext
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options)
    {
    }

    public DbSet<UserProfile> UserProfiles { get; set; }
    public DbSet<LoginAuditEntry> LoginAuditEntries { get; set; }
    public DbSet<EmailTemplate> EmailTemplates { get; set; }
    public DbSet<EmailOutboxMessage> EmailOutboxMessages { get; set; }
    public DbSet<Contact> Contacts { get; set; }
    public DbSet<Relationship> Relationships { get; set; }
    public DbSet<Company> Companies { get; set; }
    public DbSet<Note> Notes { get; set; }
    public DbSet<MediaAsset> MediaAssets { get; set; }
    public DbSet<CaptureJob> CaptureJobs { get; set; }
    public DbSet<CreditWallet> CreditWallets { get; set; }
    public DbSet<CreditTransaction> CreditTransactions { get; set; }
    public DbSet<Plan> Plans { get; set; }
    public DbSet<CreditPackage> CreditPackages { get; set; }
    public DbSet<AgentConfiguration> AgentConfigurations { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Aplicar configurações do EF Core
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(ApplicationDbContext).Assembly);

        // UserProfile
        modelBuilder.Entity<UserProfile>(entity =>
        {
            entity.ToTable("UserProfiles");
            entity.HasKey(e => e.UserId);
            
            entity.OwnsOne(e => e.Email, email =>
            {
                email.Property(e => e.Value).HasColumnName("Email").HasMaxLength(255);
                email.HasIndex(e => e.Value).IsUnique();
            });
            
            entity.OwnsOne(e => e.DisplayName, displayName =>
            {
                displayName.Property(d => d.FirstName).HasColumnName("FirstName").HasMaxLength(100);
                displayName.Property(d => d.LastName).HasColumnName("LastName").HasMaxLength(100);
            });
            
            entity.OwnsOne(e => e.KeycloakSubject, keycloakSubject =>
            {
                keycloakSubject.Property(k => k.Value).HasColumnName("KeycloakSubject").HasMaxLength(255);
                keycloakSubject.HasIndex(k => k.Value).IsUnique();
            });
            
            entity.Property(e => e.Status).HasConversion<string>().HasMaxLength(50);
            entity.Property(e => e.CreatedAt).IsRequired();
            entity.Property(e => e.PasswordResetToken).HasMaxLength(255);
        });

        // LoginAuditEntry
        modelBuilder.Entity<LoginAuditEntry>(entity =>
        {
            entity.ToTable("LoginAuditEntries");
            entity.HasKey(e => e.Id);
            
            entity.Property(e => e.UserId).IsRequired();
            entity.Property(e => e.AuthMethod).HasConversion<string>().HasMaxLength(50);
            entity.Property(e => e.IpAddress).HasMaxLength(45);
            entity.Property(e => e.UserAgent).HasMaxLength(500);
            entity.Property(e => e.OccurredAt).IsRequired();
            
            entity.HasIndex(e => e.UserId);
            entity.HasIndex(e => e.OccurredAt);
        });

        // EmailTemplate
        modelBuilder.Entity<EmailTemplate>(entity =>
        {
            entity.ToTable("EmailTemplates");
            entity.HasKey(e => e.Id);
            
            entity.Property(e => e.Name).IsRequired().HasMaxLength(200);
            entity.Property(e => e.TemplateType).HasConversion<int>();
            entity.Property(e => e.Subject).IsRequired().HasMaxLength(500);
            entity.Property(e => e.HtmlBody).IsRequired();
            entity.Property(e => e.IsActive).IsRequired();
            entity.Property(e => e.CreatedAt).IsRequired();
            
            entity.HasIndex(e => e.TemplateType);
            entity.HasIndex(e => new { e.TemplateType, e.IsActive });
        });

        // EmailOutboxMessage
        modelBuilder.Entity<EmailOutboxMessage>(entity =>
        {
            entity.ToTable("EmailOutboxMessages");
            entity.HasKey(e => e.Id);
            
            entity.Property(e => e.RecipientEmail).IsRequired().HasMaxLength(255);
            entity.Property(e => e.RecipientName).HasMaxLength(200);
            entity.Property(e => e.Subject).IsRequired().HasMaxLength(500);
            entity.Property(e => e.HtmlBody).IsRequired();
            entity.Property(e => e.Status).HasConversion<int>();
            entity.Property(e => e.CreatedAt).IsRequired();
            entity.Property(e => e.ErrorMessage).HasMaxLength(2000);
            entity.Property(e => e.RetryCount).IsRequired();
            
            entity.HasIndex(e => e.Status);
            entity.HasIndex(e => e.CreatedAt);
            entity.HasIndex(e => e.NextRetryAt);
        });

        // As configurações de Note, MediaAsset e CaptureJob são aplicadas automaticamente
        // via ApplyConfigurationsFromAssembly acima
    }
}

