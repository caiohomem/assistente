using AssistenteExecutivo.Application.Interfaces;
using AssistenteExecutivo.Domain.Interfaces;
using AssistenteExecutivo.Domain.ValueObjects;
using AssistenteExecutivo.Infrastructure.Persistence;
using AssistenteExecutivo.Infrastructure.Repositories;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using System.Reflection;
using System.Security.Cryptography;

namespace AssistenteExecutivo.Application.Tests.Helpers;

public abstract class HandlerTestBase : IDisposable
{
    protected readonly ServiceProvider ServiceProvider;
    protected readonly TestClock Clock;
    private readonly string _databaseName;

    protected HandlerTestBase()
    {
        Clock = new TestClock();
        _databaseName = $"TestDb_{Guid.NewGuid()}";

        var services = new ServiceCollection();

        // Database - In-Memory para testes
        services.AddDbContext<ApplicationDbContext>(options =>
            options.UseInMemoryDatabase(databaseName: _databaseName));

        // Register IApplicationDbContext to resolve to ApplicationDbContext
        services.AddScoped<AssistenteExecutivo.Application.Interfaces.IApplicationDbContext>(sp =>
            sp.GetRequiredService<ApplicationDbContext>());

        // Repositories
        services.AddScoped<IContactRepository, ContactRepository>();
        services.AddScoped<IRelationshipRepository, RelationshipRepository>();
        services.AddScoped<ICompanyRepository, CompanyRepository>();
        services.AddScoped<INoteRepository, NoteRepository>();
        services.AddScoped<IMediaAssetRepository, MediaAssetRepository>();
        services.AddScoped<ICaptureJobRepository, CaptureJobRepository>();
        services.AddScoped<ICreditWalletRepository, CreditWalletRepository>();
        services.AddScoped<IReminderRepository, AssistenteExecutivo.Infrastructure.Persistence.Repositories.ReminderRepository>();
        services.AddScoped<IDraftDocumentRepository, AssistenteExecutivo.Infrastructure.Persistence.Repositories.DraftDocumentRepository>();
        services.AddScoped<ITemplateRepository, AssistenteExecutivo.Infrastructure.Persistence.Repositories.TemplateRepository>();
        services.AddScoped<ILetterheadRepository, AssistenteExecutivo.Infrastructure.Persistence.Repositories.LetterheadRepository>();

        // Unit of Work
        services.AddScoped<IUnitOfWork, UnitOfWork>();

        // Clock
        services.AddSingleton<IClock>(Clock);

        // IdGenerator
        services.AddSingleton<IIdGenerator, MockIdGenerator>();

        // Mock external services
        services.AddScoped<IKeycloakService, MockKeycloakService>();
        services.AddScoped<IEmailService, MockEmailService>();
        services.AddScoped<IOcrProvider, MockOcrProvider>();
        services.AddScoped<IFileStore, MockFileStore>();
        services.AddScoped<ISpeechToTextProvider, MockSpeechToTextProvider>();
        services.AddScoped<ILLMProvider, MockLLMProvider>();

        // Mock IPublisher for domain events (no-op in tests)
        services.AddScoped<IPublisher, MockPublisher>();

        ConfigureServices(services);

        // Register all handlers from Application assembly
        RegisterHandlers(services, typeof(AssistenteExecutivo.Application.Commands.Auth.RegisterUserCommand).Assembly);

        ServiceProvider = services.BuildServiceProvider();

        using var scope = ServiceProvider.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        dbContext.Database.EnsureCreated();
    }

    protected virtual void ConfigureServices(IServiceCollection services)
    {
    }

    protected async Task<TResponse> SendAsync<TResponse>(IRequest<TResponse> request, CancellationToken cancellationToken = default)
    {
        using var scope = ServiceProvider.CreateScope();

        // Get the handler type from the request type
        var requestType = request.GetType();
        var responseType = typeof(TResponse);

        // Find handler interface: IRequestHandler<TRequest, TResponse>
        var handlerInterfaceType = typeof(IRequestHandler<,>).MakeGenericType(requestType, responseType);

        // Get handler implementation from service provider
        var handler = scope.ServiceProvider.GetRequiredService(handlerInterfaceType);

        // Call Handle method using reflection
        var handleMethod = handlerInterfaceType.GetMethod("Handle")
            ?? throw new InvalidOperationException($"Handler for {requestType.Name} does not implement Handle method");

        var result = handleMethod.Invoke(handler, new object[] { request, cancellationToken });

        // If result is Task<TResponse>, await it
        if (result is Task<TResponse> task)
        {
            return await task;
        }

        return (TResponse)result!;
    }

    protected async Task SendAsync(IRequest request, CancellationToken cancellationToken = default)
    {
        using var scope = ServiceProvider.CreateScope();

        // Get the handler type from the request type
        var requestType = request.GetType();

        // Find handler interface: IRequestHandler<TRequest>
        var handlerInterfaceType = typeof(IRequestHandler<>).MakeGenericType(requestType);

        // Get handler implementation from service provider
        var handler = scope.ServiceProvider.GetRequiredService(handlerInterfaceType);

        // Call Handle method using reflection
        var handleMethod = handlerInterfaceType.GetMethod("Handle")
            ?? throw new InvalidOperationException($"Handler for {requestType.Name} does not implement Handle method");

        var result = handleMethod.Invoke(handler, new object[] { request, cancellationToken });

        // If result is Task, await it
        if (result is Task task)
        {
            await task;
        }
    }

    private static void RegisterHandlers(IServiceCollection services, Assembly assembly)
    {
        var handlerTypes = assembly.GetTypes()
            .Where(t => t.IsClass && !t.IsAbstract)
            .Where(t => t.GetInterfaces()
                .Any(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IRequestHandler<,>)))
            .ToList();

        foreach (var handlerType in handlerTypes)
        {
            var interfaces = handlerType.GetInterfaces()
                .Where(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IRequestHandler<,>))
                .ToList();

            foreach (var interfaceType in interfaces)
            {
                services.AddScoped(interfaceType, handlerType);
            }
        }
    }

    public void Dispose()
    {
        using var scope = ServiceProvider.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        dbContext.Database.EnsureDeleted();
        ServiceProvider.Dispose();
    }
}

// Mock services for testing
internal class MockKeycloakService : IKeycloakService
{
    public Task<string> CreateRealmAsync(string realmId, string realmName, CancellationToken cancellationToken = default)
        => Task.FromResult(realmId);

    public Task<string> CreateUserAsync(string realmId, string email, string firstName, string lastName, string password, CancellationToken cancellationToken = default)
        => Task.FromResult(Guid.NewGuid().ToString());

    public Task AssignRoleAsync(string realmId, string userId, string roleName, CancellationToken cancellationToken = default)
        => Task.CompletedTask;

    public Task<string> GetAccessTokenAsync(string realmId, string username, string password, CancellationToken cancellationToken = default)
        => Task.FromResult("mock-token");

    public Task<KeycloakTokenResult> GetTokensAsync(string realmId, string username, string password, CancellationToken cancellationToken = default)
        => Task.FromResult(new KeycloakTokenResult { AccessToken = "mock-token", RefreshToken = "mock-refresh", ExpiresIn = 3600 });

    public Task<KeycloakTokenResult> RefreshTokenAsync(string realmId, string refreshToken, CancellationToken cancellationToken = default)
        => Task.FromResult(new KeycloakTokenResult { AccessToken = "mock-token", RefreshToken = "mock-refresh", ExpiresIn = 3600 });

    public Task UpdateUserPasswordAsync(string realmId, string userId, string newPassword, CancellationToken cancellationToken = default)
        => Task.CompletedTask;

    public Task<string> GetSocialLoginUrlAsync(string realmId, string provider, string redirectUri, CancellationToken cancellationToken = default)
        => Task.FromResult($"https://mock-keycloak/{realmId}/auth/{provider}?redirect={redirectUri}");

    public Task<string> GetSocialLoginUrlAsync(string realmId, string provider, string redirectUri, string? state, CancellationToken cancellationToken = default)
        => Task.FromResult($"https://mock-keycloak/{realmId}/auth/{provider}?redirect={redirectUri}&state={state}");

    public Task<KeycloakTokenResult> ExchangeAuthorizationCodeAsync(string realmId, string code, string redirectUri, CancellationToken cancellationToken = default)
        => Task.FromResult(new KeycloakTokenResult { AccessToken = "mock-token", RefreshToken = "mock-refresh", ExpiresIn = 3600 });

    public Task<KeycloakUserInfo> GetUserInfoAsync(string realmId, string accessToken, CancellationToken cancellationToken = default)
        => Task.FromResult(new KeycloakUserInfo { Sub = Guid.NewGuid().ToString(), Email = "test@example.com" });

    public Task LogoutAsync(string realmId, string refreshToken, CancellationToken cancellationToken = default)
        => Task.CompletedTask;

    public Task ConfigureGoogleIdentityProviderAsync(string realmId, string clientId, string clientSecret, CancellationToken cancellationToken = default)
        => Task.CompletedTask;

    public Task EnsureClientExistsAsync(string realmId, CancellationToken cancellationToken = default)
        => Task.CompletedTask;

    public Task ConfigureRealmProvidersAsync(string realmId, CancellationToken cancellationToken = default)
        => Task.CompletedTask;

    public Task<string?> GetUserIdByEmailAsync(string realmId, string email, CancellationToken cancellationToken = default)
        => Task.FromResult<string?>(Guid.NewGuid().ToString());

    public Task DeleteUserAsync(string realmId, string userId, CancellationToken cancellationToken = default)
        => Task.CompletedTask;
}

internal class MockEmailService : IEmailService
{
    public Task SendEmailWithTemplateAsync(
        Domain.Notifications.EmailTemplateType templateType,
        string recipientEmail,
        string recipientName,
        Dictionary<string, object> templateValues,
        CancellationToken cancellationToken = default)
    {
        return Task.CompletedTask;
    }
}

internal class MockIdGenerator : IIdGenerator
{
    public Guid NewGuid() => Guid.NewGuid();
    public string NewId() => Guid.NewGuid().ToString();
}

internal class MockOcrProvider : IOcrProvider
{
    public Task<OcrExtract> ExtractFieldsAsync(byte[] imageBytes, string mimeType, CancellationToken cancellationToken = default)
    {
        // Return mock OCR data
        return Task.FromResult(new OcrExtract(
            name: "João Silva",
            email: "joao.silva@example.com",
            phone: "11987654321",
            company: "Tech Corp",
            jobTitle: "Software Engineer"
        ));
    }
}

internal class MockFileStore : IFileStore
{
    private readonly Dictionary<string, byte[]> _storage = new();

    public Task<string> StoreAsync(byte[] fileBytes, string fileName, string mimeType, CancellationToken cancellationToken = default)
    {
        var key = $"mock/{Guid.NewGuid()}/{fileName}";
        _storage[key] = fileBytes;
        return Task.FromResult(key);
    }

    public Task<byte[]> GetAsync(string storageKey, CancellationToken cancellationToken = default)
    {
        return Task.FromResult(_storage.TryGetValue(storageKey, out var bytes) ? bytes : Array.Empty<byte>());
    }

    public Task DeleteAsync(string storageKey, CancellationToken cancellationToken = default)
    {
        _storage.Remove(storageKey);
        return Task.CompletedTask;
    }

    public Task<string> ComputeHashAsync(byte[] fileBytes, CancellationToken cancellationToken = default)
    {
        using var sha256 = SHA256.Create();
        var hash = sha256.ComputeHash(fileBytes);
        var hashString = Convert.ToHexString(hash).ToLowerInvariant();
        return Task.FromResult(hashString);
    }
}

internal class MockSpeechToTextProvider : ISpeechToTextProvider
{
    public Task<Transcript> TranscribeAsync(byte[] audioBytes, string mimeType, CancellationToken cancellationToken = default)
    {
        return Task.FromResult(new Transcript("This is a mock transcript from audio processing."));
    }
}

internal class MockLLMProvider : ILLMProvider
{
    public Task<AudioProcessingResult> SummarizeAndExtractTasksAsync(string transcript, CancellationToken cancellationToken = default)
    {
        return Task.FromResult(new AudioProcessingResult
        {
            Summary = "Mock summary of the audio transcript.",
            Tasks = new List<ExtractedTask>
            {
                new ExtractedTask("Mock task 1", null, "normal")
            }
        });
    }
}

internal class MockPublisher : IPublisher
{
    public Task Publish(object notification, CancellationToken cancellationToken = default)
    {
        // No-op for tests - domain events are not processed
        return Task.CompletedTask;
    }

    public Task Publish<TNotification>(TNotification notification, CancellationToken cancellationToken = default) where TNotification : INotification
    {
        // No-op for tests - domain events are not processed
        return Task.CompletedTask;
    }
}
