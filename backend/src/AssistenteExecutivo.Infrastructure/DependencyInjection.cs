using AssistenteExecutivo.Application.Interfaces;
using AssistenteExecutivo.Domain.Interfaces;
using AssistenteExecutivo.Infrastructure.HttpClients;
using AssistenteExecutivo.Infrastructure.Persistence;
using AssistenteExecutivo.Infrastructure.Repositories;
using AssistenteExecutivo.Infrastructure.Services;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Npgsql;

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
            // Detectar se é PostgreSQL ou SQL Server pela connection string
            var isPostgreSQL = connectionString.StartsWith("postgresql://", StringComparison.OrdinalIgnoreCase) ||
                               connectionString.StartsWith("postgres://", StringComparison.OrdinalIgnoreCase) ||
                               (connectionString.Contains("Host=", StringComparison.OrdinalIgnoreCase) && 
                                !connectionString.Contains("Trusted_Connection", StringComparison.OrdinalIgnoreCase));
            
            // Se for URL do PostgreSQL, converter para formato de parâmetros
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
            
            if (isPostgreSQL)
            {
                // PostgreSQL (Neon)
                // Usar connection string convertida (já está no formato de parâmetros se era URL)
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
            }
            else
            {
                // SQL Server (fallback para compatibilidade)
                options.UseSqlServer(connectionString, sqlOptions =>
                {
                    // Habilitar retry logic para falhas transitórias
                    sqlOptions.EnableRetryOnFailure(
                        maxRetryCount: 3,
                        maxRetryDelay: TimeSpan.FromSeconds(5),
                        errorNumbersToAdd: null);
                });
            }
            
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
        services.AddScoped<IContactRepository, ContactRepository>();
        services.AddScoped<IRelationshipRepository, RelationshipRepository>();
        services.AddScoped<ICompanyRepository, CompanyRepository>();
        services.AddScoped<INoteRepository, NoteRepository>();
        services.AddScoped<IMediaAssetRepository, MediaAssetRepository>();
        services.AddScoped<ICaptureJobRepository, CaptureJobRepository>();
        services.AddScoped<ICreditWalletRepository, CreditWalletRepository>();
        services.AddScoped<IPlanRepository, PlanRepository>();
        services.AddScoped<IAgentConfigurationRepository, AgentConfigurationRepository>();

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

        // Ollama HTTP Client
        var ollamaBaseUrl = configuration["Ollama:BaseUrl"] ?? "http://localhost:11434";
        services.AddHttpClient<OllamaClient>(client =>
        {
            client.BaseAddress = new Uri(ollamaBaseUrl);
            client.Timeout = TimeSpan.FromMinutes(10); // Modelos podem demorar mais
            client.DefaultRequestHeaders.Add("Accept", "application/json");
        });

        // PaddleOCR FastAPI Client (local)
        var paddleOcrBaseUrl = configuration["Ocr:PaddleOcr:BaseUrl"] ?? "http://localhost:8001";
        services.AddHttpClient<PaddleOcrApiClient>(client =>
        {
            client.BaseAddress = new Uri(paddleOcrBaseUrl);
            client.Timeout = TimeSpan.FromMinutes(2);
            client.DefaultRequestHeaders.Add("Accept", "application/json");
        });

        // Keycloak provisioning runs as a hosted service (singleton).
        // Keep a single instance that can be used both as IHostedService and via the app interface.
        services.AddSingleton<KeycloakAdminProvisioner>();
        services.AddSingleton<IKeycloakAdminProvisioner>(sp => sp.GetRequiredService<KeycloakAdminProvisioner>());
        services.AddSingleton<IHostedService>(sp => sp.GetRequiredService<KeycloakAdminProvisioner>());

        services.AddScoped<IEmailService, EmailService>();
        services.AddSingleton<IClock, SystemClock>();

        // External service providers
        
        // Speech-to-Text Provider - Configurable via appsettings.json
        // Options: "Stub" (default), "Ollama", "Whisper"
        var speechToTextProvider = configuration["Whisper:Provider"] ?? "Stub";
        switch (speechToTextProvider)
        {
            case "Ollama":
            case "Whisper":
                var whisperApiUrl = configuration["Whisper:ApiUrl"];
                if (!string.IsNullOrWhiteSpace(whisperApiUrl))
                {
                    // Se houver API URL, criar HttpClient nomeado para API externa
                    services.AddHttpClient("WhisperApi", client =>
                    {
                        client.BaseAddress = new Uri(whisperApiUrl);
                        client.Timeout = TimeSpan.FromMinutes(10);
                    });
                    
                    // Registrar provider com HttpClient via factory
                    services.AddScoped<ISpeechToTextProvider>(sp =>
                    {
                        var httpClientFactory = sp.GetRequiredService<IHttpClientFactory>();
                        var httpClient = httpClientFactory.CreateClient("WhisperApi");
                        var config = sp.GetRequiredService<IConfiguration>();
                        var logger = sp.GetRequiredService<ILogger<OllamaWhisperProvider>>();
                        var ollamaClient = sp.GetService<OllamaClient>();
                        return new OllamaWhisperProvider(config, logger, ollamaClient, httpClient, httpClientFactory);
                    });
                }
                else
                {
                    // Se não houver API URL, registrar normalmente (vai falhar se tentar usar API)
                    services.AddScoped<ISpeechToTextProvider, OllamaWhisperProvider>();
                }
                break;
            case "Stub":
            default:
                services.AddScoped<ISpeechToTextProvider, StubSpeechToTextProvider>();
                break;
        }
        
        // OCR Provider - Configurable via appsettings.json
        // Options: "Stub" (default), "Ollama", "PaddleOcr", "Azure", "GoogleCloud", "Aws"
        var ocrProvider = configuration["Ocr:Provider"] ?? "Stub";
        switch (ocrProvider)
        {
            case "Ollama":
                services.AddScoped<IOcrProvider, OllamaOcrProvider>();
                break;
            case "PaddleOcr":
                services.AddScoped<IOcrProvider, PaddleOcrProvider>();
                break;
            case "Azure":
                // TODO: Implement AzureComputerVisionOcrProvider
                // var azureEndpoint = configuration["Ocr:Azure:Endpoint"];
                // var azureApiKey = configuration["Ocr:Azure:ApiKey"];
                // services.AddScoped<IOcrProvider, AzureComputerVisionOcrProvider>();
                // For now, fallback to Stub
                services.AddScoped<IOcrProvider, StubOcrProvider>();
                break;
            case "GoogleCloud":
                // TODO: Implement GoogleCloudVisionOcrProvider
                // var projectId = configuration["Ocr:GoogleCloud:ProjectId"];
                // services.AddScoped<IOcrProvider, GoogleCloudVisionOcrProvider>();
                // For now, fallback to Stub
                services.AddScoped<IOcrProvider, StubOcrProvider>();
                break;
            case "Aws":
                // TODO: Implement AwsTextractOcrProvider
                // var region = configuration["Ocr:Aws:Region"];
                // services.AddScoped<IOcrProvider, AwsTextractOcrProvider>();
                // For now, fallback to Stub
                services.AddScoped<IOcrProvider, StubOcrProvider>();
                break;
            case "Stub":
            default:
                services.AddScoped<IOcrProvider, StubOcrProvider>();
                break;
        }
        
        // LLM Provider - Configurable via appsettings.json
        // Options: "Stub" (default), "Ollama"
        var llmProvider = configuration["Ollama:LLM:Provider"] ?? "Stub";
        switch (llmProvider)
        {
            case "Ollama":
                services.AddScoped<ILLMProvider, OllamaLLMProvider>();
                break;
            case "Stub":
            default:
                services.AddScoped<ILLMProvider, StubLLMProvider>();
                break;
        }
        
        // OCR Field Refinement Service - Usa Qwen para melhorar associação de campos
        services.AddScoped<IOcrFieldRefinementService, QwenOcrRefinementService>();
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
