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

namespace AssistenteExecutivo.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        // Database
        var connectionString = configuration.GetConnectionString("DefaultConnection")
            ?? "Server=localhost;Database=AssistenteExecutivo;Trusted_Connection=True;TrustServerCertificate=True;";

        services.AddDbContext<ApplicationDbContext>(options =>
        {
            options.UseSqlServer(connectionString, sqlOptions =>
            {
                // Habilitar retry logic para falhas transitórias
                sqlOptions.EnableRetryOnFailure(
                    maxRetryCount: 3,
                    maxRetryDelay: TimeSpan.FromSeconds(5),
                    errorNumbersToAdd: null);
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
        services.AddScoped<IContactRepository, ContactRepository>();
        services.AddScoped<IRelationshipRepository, RelationshipRepository>();
        services.AddScoped<ICompanyRepository, CompanyRepository>();
        services.AddScoped<INoteRepository, NoteRepository>();
        services.AddScoped<IMediaAssetRepository, MediaAssetRepository>();
        services.AddScoped<ICaptureJobRepository, CaptureJobRepository>();
        services.AddScoped<ICreditWalletRepository, CreditWalletRepository>();
        services.AddScoped<IPlanRepository, PlanRepository>();

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
