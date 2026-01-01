using AssistenteExecutivo.Application.Interfaces;
using AssistenteExecutivo.Domain.Interfaces;
using AssistenteExecutivo.Infrastructure.Persistence;
using AssistenteExecutivo.Infrastructure.Persistence.Repositories;
using AssistenteExecutivo.Infrastructure.Repositories;
using AssistenteExecutivo.Infrastructure.Services;
using AssistenteExecutivo.Infrastructure.Services.N8n;
using AssistenteExecutivo.Infrastructure.Services.OpenAI;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace AssistenteExecutivo.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        // Database
        var connectionString = configuration.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException("ConnectionString 'DefaultConnection' não configurada");

        // Log da connection string (sem senha) para debug
        var connectionStringForLog = connectionString.Contains("Password=", StringComparison.OrdinalIgnoreCase)
            ? connectionString.Substring(0, connectionString.IndexOf("Password=", StringComparison.OrdinalIgnoreCase)) + "Password=***"
            : connectionString.Substring(0, Math.Min(100, connectionString.Length));
        System.Diagnostics.Debug.WriteLine($"[DB] Connection String: {connectionStringForLog}");

        services.AddDbContext<ApplicationDbContext>(options =>
        {
            // Converter URL do PostgreSQL para formato de parâmetros se necessário
            string finalConnectionString = connectionString;
            if (connectionString.StartsWith("postgresql://", StringComparison.OrdinalIgnoreCase) ||
                connectionString.StartsWith("postgres://", StringComparison.OrdinalIgnoreCase))
            {
                // Converter URL para formato de parâmetros
                try
                {
                    var uri = new Uri(connectionString);
                    var builder = new Npgsql.NpgsqlConnectionStringBuilder
                    {
                        Host = uri.Host,
                        Port = uri.Port != -1 ? uri.Port : 5432,
                        Database = uri.AbsolutePath.TrimStart('/'),
                        Username = uri.UserInfo.Split(':')[0],
                        Password = uri.UserInfo.Split(':').Length > 1 ? uri.UserInfo.Split(':')[1] : "",
                        SslMode = Npgsql.SslMode.Require
                    };

                    // Adicionar parâmetros da query string
                    if (!string.IsNullOrEmpty(uri.Query))
                    {
                        var query = uri.Query.TrimStart('?');
                        var pairs = query.Split('&');
                        foreach (var pair in pairs)
                        {
                            var parts = pair.Split('=');
                            if (parts.Length == 2)
                            {
                                var key = parts[0].ToLowerInvariant();
                                var value = parts[1];

                                if (key == "sslmode")
                                {
                                    if (Enum.TryParse<Npgsql.SslMode>(value, true, out var mode))
                                        builder.SslMode = mode;
                                }
                            }
                        }
                    }

                    finalConnectionString = builder.ConnectionString;
                    System.Diagnostics.Debug.WriteLine($"[DB] Converted URL to parameter format");
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"[DB] Error converting URL: {ex.Message}");
                    throw new InvalidOperationException("Erro ao converter connection string de URL para formato de parâmetros. Use o formato: Host=...;Database=...;Username=...;Password=...;SSL Mode=Require;", ex);
                }
            }

            // PostgreSQL (Neon)
            options.UseNpgsql(finalConnectionString, npgsqlOptions =>
            {
                // Habilitar retry logic para falhas transitórias
                // Aumentado para conexões com Neon que podem ter latência maior
                npgsqlOptions.EnableRetryOnFailure(
                    maxRetryCount: 5,
                    maxRetryDelay: TimeSpan.FromSeconds(10),
                    errorCodesToAdd: null);

                // Timeout de comando aumentado para operações que podem demorar
                npgsqlOptions.CommandTimeout(60);
            });

            // Desabilitar detecção de concorrência otimista automática
            // (usaremos apenas quando explicitamente configurado com RowVersion/Timestamp)
            options.EnableSensitiveDataLogging(false);
            options.EnableServiceProviderCaching();
        });

        // Register IApplicationDbContext to resolve to ApplicationDbContext
        services.AddScoped<AssistenteExecutivo.Application.Interfaces.IApplicationDbContext>(sp =>
            sp.GetRequiredService<ApplicationDbContext>());

        // Repositories
        services.AddScoped<IEmailTemplateRepository, EmailTemplateRepository>();
        services.AddScoped<IUserProfileRepository, UserProfileRepository>();
        services.AddScoped<IContactRepository, ContactRepository>();
        services.AddScoped<IRelationshipRepository, RelationshipRepository>();
        services.AddScoped<ICompanyRepository, CompanyRepository>();
        services.AddScoped<INoteRepository, NoteRepository>();
        services.AddScoped<IMediaAssetRepository, MediaAssetRepository>();
        services.AddScoped<ICaptureJobRepository, CaptureJobRepository>();
        services.AddScoped<ICreditWalletRepository, CreditWalletRepository>();
        services.AddScoped<ICreditPackageRepository, CreditPackageRepository>();
        services.AddScoped<IPlanRepository, PlanRepository>();
        services.AddScoped<IAgentConfigurationRepository, AgentConfigurationRepository>();
        services.AddScoped<IReminderRepository, ReminderRepository>();
        services.AddScoped<IDraftDocumentRepository, DraftDocumentRepository>();
        services.AddScoped<ITemplateRepository, TemplateRepository>();
        services.AddScoped<ILetterheadRepository, LetterheadRepository>();
        services.AddScoped<IWorkflowRepository, WorkflowRepository>();
        services.AddScoped<IWorkflowExecutionRepository, WorkflowExecutionRepository>();

        // n8n Services
        services.AddScoped<IWorkflowSpecValidator, WorkflowSpecValidator>();
        services.AddScoped<IWorkflowCompiler, WorkflowCompiler>();
        services.AddScoped<IN8nProvider, N8nProvider>();

        // Unit of Work
        services.AddScoped<IUnitOfWork, UnitOfWork>();

        // Services
        var keycloakBaseUrl = configuration["Keycloak:BaseUrl"]
            ?? throw new InvalidOperationException("Keycloak:BaseUrl não configurado em appsettings");

        services.AddHttpClient<IKeycloakService, KeycloakService>(client =>
        {
            client.BaseAddress = new Uri(keycloakBaseUrl);
            client.Timeout = TimeSpan.FromMinutes(5);
            client.DefaultRequestHeaders.Add("Accept", "application/json");
        });


        // Keycloak provisioning runs as a hosted service (singleton).
        // Keep a single instance that can be used both as IHostedService and via the app interface.
        services.AddSingleton<KeycloakAdminProvisioner>();
        services.AddSingleton<IKeycloakAdminProvisioner>(sp => sp.GetRequiredService<KeycloakAdminProvisioner>());
        services.AddSingleton<IHostedService>(sp => sp.GetRequiredService<KeycloakAdminProvisioner>());

        services.AddHttpClient<EmailService>();
        services.AddScoped<IEmailService>(sp =>
        {
            var httpClientFactory = sp.GetRequiredService<IHttpClientFactory>();
            var emailTemplateRepository = sp.GetRequiredService<IEmailTemplateRepository>();
            var configuration = sp.GetRequiredService<IConfiguration>();
            var logger = sp.GetRequiredService<ILogger<EmailService>>();
            return new EmailService(emailTemplateRepository, configuration, logger, httpClientFactory);
        });
        services.AddSingleton<IClock, SystemClock>();
        services.AddScoped<AudioTrimmer>();

        // External service providers - OpenAI only

        // Speech-to-Text Provider - OpenAI Whisper
        services.AddScoped<ISpeechToTextProvider>(sp =>
        {
            var config = sp.GetRequiredService<IConfiguration>();
            var httpClientFactory = sp.GetRequiredService<IHttpClientFactory>();
            var logger = sp.GetRequiredService<ILogger<OpenAISpeechToTextProvider>>();
            logger.LogInformation("Usando OpenAI Speech-to-Text Provider (Whisper)");
            return new OpenAISpeechToTextProvider(config, httpClientFactory, logger);
        });

        // OCR Provider - OpenAI Vision
        services.AddScoped<IOcrProvider>(sp =>
        {
            var config = sp.GetRequiredService<IConfiguration>();
            var httpClientFactory = sp.GetRequiredService<IHttpClientFactory>();
            var logger = sp.GetRequiredService<ILogger<OpenAIOcrProvider>>();
            var configurationRepository = sp.GetRequiredService<IAgentConfigurationRepository>();
            logger.LogInformation("Usando OpenAI OCR Provider");
            return new OpenAIOcrProvider(config, httpClientFactory, logger, configurationRepository);
        });

        // LLM Provider - OpenAI GPT
        services.AddScoped<ILLMProvider>(sp =>
        {
            var config = sp.GetRequiredService<IConfiguration>();
            var httpClientFactory = sp.GetRequiredService<IHttpClientFactory>();
            var logger = sp.GetRequiredService<ILogger<OpenAILLMProvider>>();
            var configurationRepository = sp.GetRequiredService<IAgentConfigurationRepository>();
            logger.LogInformation("Usando OpenAI LLM Provider");
            return new OpenAILLMProvider(config, httpClientFactory, logger, configurationRepository);
        });

        // Text-to-Speech Provider - OpenAI TTS
        var textToSpeechEnabledValue = configuration["OpenAI:TextToSpeech:Enabled"];
        var textToSpeechEnabled = string.IsNullOrWhiteSpace(textToSpeechEnabledValue) ||
                                  (bool.TryParse(textToSpeechEnabledValue, out var enabled) && enabled);
        if (textToSpeechEnabled)
        {
            services.AddScoped<ITextToSpeechProvider>(sp =>
            {
                var config = sp.GetRequiredService<IConfiguration>();
                var httpClientFactory = sp.GetRequiredService<IHttpClientFactory>();
                var logger = sp.GetRequiredService<ILogger<OpenAITextToSpeechProvider>>();
                return new OpenAITextToSpeechProvider(config, httpClientFactory, logger);
            });
        }
        else
        {
            // Se desabilitado, usar stub (apenas para desenvolvimento)
            services.AddScoped<ITextToSpeechProvider, StubTextToSpeechProvider>();
        }
        services.AddScoped<IFileStore, StubFileStore>();
        services.AddSingleton<IIdGenerator, GuidIdGenerator>();

        // Memory Cache (para cache de tokens do Keycloak)
        services.AddMemoryCache();

        // MediatR - Registra handlers automaticamente do assembly Application
        services.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(
            typeof(AssistenteExecutivo.Application.Commands.Auth.RegisterUserCommand).Assembly));

        return services;
    }
}
