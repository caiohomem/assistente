using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using AssistenteExecutivo.Application.Interfaces;
using AssistenteExecutivo.Infrastructure;
using AssistenteExecutivo.Infrastructure.Persistence;
using AssistenteExecutivo.Api.Auth;
using AssistenteExecutivo.Api.Security;
using AssistenteExecutivo.Api.Extensions;
using AssistenteExecutivo.Api.Middleware;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.IdentityModel.Tokens;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.SqlServer;
using Microsoft.OpenApi;
using Serilog;
using Serilog.Events;
using Serilog.Sinks.MSSqlServer;
using Microsoft.OpenApi;

// Configurar Serilog antes de criar o builder
var configuration = new ConfigurationBuilder()
    .SetBasePath(Directory.GetCurrentDirectory())
    .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
    .AddJsonFile($"appsettings.{Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Production"}.json", optional: true)
    .AddEnvironmentVariables()
    .Build();

// Obter connection string para o sink do SQL Server
var connectionString = configuration.GetConnectionString("DefaultConnection")
    ?? "Server=localhost;Database=AssistenteExecutivo;Trusted_Connection=True;TrustServerCertificate=True;";

// Configurar colunas customizadas para o SQL Server
var columnOptions = new MSSqlServerSinkOptions
{
    TableName = "Logs",
    AutoCreateSqlTable = false, // Assumimos que a tabela já foi criada pelo script SQL
    SchemaName = "dbo"
};

var columnOptionsObj = new ColumnOptions();
columnOptionsObj.Store.Add(StandardColumn.LogEvent);
columnOptionsObj.Store.Remove(StandardColumn.Properties);
columnOptionsObj.Store.Remove(StandardColumn.MessageTemplate);

// Adicionar colunas customizadas
// Nota: Tamanhos limitados para permitir criação de índices (NVARCHAR(MAX) não pode ser indexado)
columnOptionsObj.AdditionalColumns = new[]
{
    new SqlColumn("SourceContext", System.Data.SqlDbType.NVarChar, dataLength: 512),
    new SqlColumn("RequestPath", System.Data.SqlDbType.NVarChar, dataLength: 512),
    new SqlColumn("RequestMethod", System.Data.SqlDbType.NVarChar, dataLength: 10),
    new SqlColumn("StatusCode", System.Data.SqlDbType.Int, allowNull: true),
    new SqlColumn("Elapsed", System.Data.SqlDbType.Float, allowNull: true),
    new SqlColumn("UserName", System.Data.SqlDbType.NVarChar, dataLength: 256),
    new SqlColumn("MachineName", System.Data.SqlDbType.NVarChar, dataLength: 256),
    new SqlColumn("Environment", System.Data.SqlDbType.NVarChar, dataLength: 50)
};

Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Information()
    .MinimumLevel.Override("Microsoft", LogEventLevel.Warning)
    .MinimumLevel.Override("Microsoft.AspNetCore", LogEventLevel.Warning)
    .MinimumLevel.Override("Microsoft.EntityFrameworkCore", LogEventLevel.Warning)
    .Enrich.FromLogContext()
    .Enrich.WithMachineName()
    .Enrich.WithEnvironmentName()
    .Enrich.WithProperty("Environment", Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Production")
    .WriteTo.Console(
        outputTemplate: "[{Timestamp:HH:mm:ss} {Level:u3}] {Message:lj} {Properties:j}{NewLine}{Exception}")
    .WriteTo.MSSqlServer(
        connectionString: connectionString,
        sinkOptions: columnOptions,
        columnOptions: columnOptionsObj,
        restrictedToMinimumLevel: LogEventLevel.Warning) // Apenas Warning, Error e Fatal vão para o banco
    .CreateLogger();

try
{
    Log.Information("Iniciando aplicação AssistenteExecutivo.Api");

    var builder = WebApplication.CreateBuilder(args);
    
    // Substituir o logger padrão pelo Serilog
    builder.Host.UseSerilog();

    // Forwarded headers (X-Forwarded-Proto/Host) para rodar atrA¡s de proxy/tunnel HTTPS.
    // Importante para gerar URLs corretas (redirect_uri, cookies Secure, etc).
    builder.Services.Configure<ForwardedHeadersOptions>(options =>
    {
        options.ForwardedHeaders =
            ForwardedHeaders.XForwardedFor |
            ForwardedHeaders.XForwardedProto |
            ForwardedHeaders.XForwardedHost;

        // Em DEV/HML com tunnel (ngrok/cloudflared), os IPs do proxy variam.
        // Aceitar forwarded headers de qualquer origem.
        options.KnownNetworks.Clear();
        options.KnownProxies.Clear();
    });

    // Add services to the container
    builder.Services.AddControllers();
    builder.Services.AddEndpointsApiExplorer();
    builder.Services.AddSwaggerGen(c =>
    {
        c.SwaggerDoc("v1", new OpenApiInfo 
        { 
            Title = "Assistente Executivo API", 
            Version = "v1" 
        });
    });

    // Localization
    builder.Services.AddLocalizationConfiguration();

    // Infrastructure
    builder.Services.AddInfrastructure(builder.Configuration);

    // Auth
    // - BFF: session-backed principal (default for web)
    // - Mobile/API: JWT Bearer tokens issued by Keycloak
    builder.Services
    .AddAuthentication(options =>
    {
        options.DefaultScheme = BffSessionAuthenticationDefaults.Scheme;
        options.DefaultChallengeScheme = BffSessionAuthenticationDefaults.Scheme;
    })
    .AddScheme<Microsoft.AspNetCore.Authentication.AuthenticationSchemeOptions, BffSessionAuthenticationHandler>(
        BffSessionAuthenticationDefaults.Scheme,
        _ => { })
    .AddJwtBearer(JwtBearerDefaults.AuthenticationScheme, options =>
    {
        // Use PublicBaseUrl for token validation (mobile clients use public URL)
        // Fallback to BaseUrl if PublicBaseUrl not set
        var publicBaseUrl = builder.Configuration["Keycloak:PublicBaseUrl"];
        var keycloakBaseUrl = !string.IsNullOrWhiteSpace(publicBaseUrl) 
            ? publicBaseUrl 
            : builder.Configuration["Keycloak:BaseUrl"] 
                ?? throw new InvalidOperationException("Keycloak:PublicBaseUrl ou Keycloak:BaseUrl deve estar configurado em appsettings");
        keycloakBaseUrl = keycloakBaseUrl.TrimEnd('/');
        var realm = builder.Configuration["Keycloak:Realm"] ?? "assistenteexecutivo";
        var authority = $"{keycloakBaseUrl}/realms/{realm}";

        // Ensure authority uses HTTPS (Keycloak will be configured to emit HTTPS issuers)
        if (authority.StartsWith("http://", StringComparison.OrdinalIgnoreCase))
        {
            authority = authority.Replace("http://", "https://", StringComparison.OrdinalIgnoreCase);
        }

        Console.WriteLine($"[JWT] Authority: {authority}");
        Console.WriteLine($"[JWT] RequireHttpsMetadata: true");

        options.Authority = authority;
        options.RequireHttpsMetadata = true; // Always require HTTPS for production
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = authority, // Only HTTPS issuer
            ValidateAudience = false, // Keycloak tokens can have multiple audiences; validate with policies if needed
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            NameClaimType = "preferred_username",
            RoleClaimType = "roles"
        };

        // Log JWT validation errors
        options.Events = new Microsoft.AspNetCore.Authentication.JwtBearer.JwtBearerEvents
        {
            OnAuthenticationFailed = context =>
            {
                Console.WriteLine($"[JWT] ===== AUTH FAILED =====");
                Console.WriteLine($"[JWT] Path: {context.Request.Path}");
                Console.WriteLine($"[JWT] Method: {context.Request.Method}");
                Console.WriteLine($"[JWT] Exception Type: {context.Exception.GetType().Name}");
                Console.WriteLine($"[JWT] Exception Message: {context.Exception.Message}");
                if (context.Exception.InnerException != null)
                {
                    Console.WriteLine($"[JWT] Inner Exception: {context.Exception.InnerException.GetType().Name}: {context.Exception.InnerException.Message}");
                }
                
                var authHeader = context.Request.Headers.Authorization.FirstOrDefault();
                if (!string.IsNullOrEmpty(authHeader))
                {
                    var tokenPart = authHeader.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase) 
                        ? authHeader.Substring(7) 
                        : authHeader;
                    var tokenPrefix = tokenPart.Length > 50 ? tokenPart.Substring(0, 50) : tokenPart;
                    Console.WriteLine($"[JWT] Token prefix: {tokenPrefix}...");
                    
                    // Try to decode token to see issuer
                    try
                    {
                        var handler = new JwtSecurityTokenHandler();
                        var token = handler.ReadJwtToken(tokenPart);
                        Console.WriteLine($"[JWT] Token issuer: {token.Issuer}");
                        Console.WriteLine($"[JWT] Expected issuer: {authority}");
                        Console.WriteLine($"[JWT] Issuer match: {token.Issuer == authority}");
                        Console.WriteLine($"[JWT] Token expires: {token.ValidTo:O}");
                        Console.WriteLine($"[JWT] Token expired: {token.ValidTo < DateTime.UtcNow}");
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"[JWT] Failed to decode token: {ex.Message}");
                    }
                }
                else
                {
                    Console.WriteLine($"[JWT] No Authorization header found");
                }
                Console.WriteLine($"[JWT] =========================");
                return Task.CompletedTask;
            },
            OnTokenValidated = context =>
            {
                Console.WriteLine($"[JWT] ✓ Token validated for: {context.Principal?.Identity?.Name}");
                var issuer = context.Principal?.FindFirst("iss")?.Value;
                Console.WriteLine($"[JWT] ✓ Token issuer: {issuer}");
                return Task.CompletedTask;
            },
            OnChallenge = context =>
            {
                Console.WriteLine($"[JWT] ===== CHALLENGE =====");
                Console.WriteLine($"[JWT] Path: {context.Request.Path}");
                Console.WriteLine($"[JWT] Error: {context.Error}");
                Console.WriteLine($"[JWT] ErrorDescription: {context.ErrorDescription}");
                Console.WriteLine($"[JWT] ======================");
                return Task.CompletedTask;
            }
        };
    });
    builder.Services.AddAuthorization();

    // Session (para BFF) - Usando SQL Server para persistir sessões entre reinicializações
    var sessionConnectionString = builder.Configuration.GetConnectionString("DefaultConnection")
        ?? throw new InvalidOperationException("ConnectionString 'DefaultConnection' não configurada");
    
    builder.Services.AddDistributedSqlServerCache(options =>
    {
        options.ConnectionString = sessionConnectionString;
        options.SchemaName = "dbo";
        options.TableName = "SessionCache";
        // Expiração padrão de 20 minutos (as sessões individuais têm timeout de 30 minutos)
        options.DefaultSlidingExpiration = TimeSpan.FromMinutes(20);
    });
    
    builder.Services.AddSession(options =>
    {
        options.IdleTimeout = TimeSpan.FromMinutes(30);
        options.Cookie.HttpOnly = true;
        options.Cookie.IsEssential = true;
        // Usar SameSite=None com Secure para funcionar em redirects cross-subdomain
        // Isso permite que o cookie seja enviado quando o redirect vai de assistente-api para assistente
        options.Cookie.SameSite = SameSiteMode.None;
        // Sempre usar Secure quando SameSite=None (requisito do navegador)
        // Se estiver usando HTTPS (via PublicBaseUrl), sempre Secure
        var apiPublicBaseUrl = builder.Configuration["Api:PublicBaseUrl"];
        var isHttps = !string.IsNullOrWhiteSpace(apiPublicBaseUrl) && apiPublicBaseUrl.StartsWith("https://", StringComparison.OrdinalIgnoreCase);
        options.Cookie.SecurePolicy = isHttps ? CookieSecurePolicy.Always : CookieSecurePolicy.SameAsRequest;
        options.Cookie.Name = "ae.sid";
        
        // Configurar Domain para funcionar entre subdomínios quando usando PublicBaseUrl
        if (!string.IsNullOrWhiteSpace(apiPublicBaseUrl) && Uri.TryCreate(apiPublicBaseUrl, UriKind.Absolute, out var apiUri))
        {
            // Extrair o domínio base (ex: callback-local-cchagas.xyz)
            var host = apiUri.Host;
            // Se for um subdomínio (ex: assistente-api.callback-local-cchagas.xyz), usar o domínio base
            var parts = host.Split('.');
            if (parts.Length >= 2)
            {
                // Pegar os últimos 2 ou 3 segmentos (ex: callback-local-cchagas.xyz ou exemplo.com.br)
                var domainBase = parts.Length >= 3 && parts[parts.Length - 2].Length <= 3 
                    ? string.Join(".", parts.Skip(parts.Length - 3)) // Para .com.br, .co.uk, etc
                    : string.Join(".", parts.Skip(parts.Length - 2)); // Para .com, .xyz, etc
                
                // Configurar Domain com ponto inicial para funcionar em todos os subdomínios
                options.Cookie.Domain = $".{domainBase}";
            }
        }
    });

    // CORS
    builder.Services.AddCors(options =>
    {
        options.AddDefaultPolicy(policy =>
        {
            var frontendBaseUrl = builder.Configuration["Frontend:BaseUrl"] ?? "http://localhost:3000";
            var frontendPublicBaseUrl = builder.Configuration["Frontend:PublicBaseUrl"];
            
            var origins = new List<string> { frontendBaseUrl };
            if (!string.IsNullOrWhiteSpace(frontendPublicBaseUrl) && frontendPublicBaseUrl != frontendBaseUrl)
            {
                origins.Add(frontendPublicBaseUrl);
            }
            
            policy.WithOrigins(origins.ToArray())
                .AllowAnyMethod()
                .AllowAnyHeader()
                .AllowCredentials();
        });
    });

        var app = builder.Build();

    app.UseForwardedHeaders();

    // Initialize database and seed data
    using (var scope = app.Services.CreateScope())
    {
        var services = scope.ServiceProvider;
        try
        {
            var context = services.GetRequiredService<ApplicationDbContext>();
            
            // Apply migrations
            await context.Database.MigrateAsync();
            
            // Seed email templates
            await DatabaseSeeder.SeedEmailTemplatesAsync(context);
            
            // Keycloak provisioning será feito pelo HostedService (KeycloakAdminProvisioner)
            // Não precisa chamar manualmente aqui
        }
        catch (Exception ex)
        {
            var logger = services.GetRequiredService<ILogger<Program>>();
            logger.LogError(ex, "Erro ao inicializar banco de dados ou provisionar Keycloak");
        }
    }

    // Configure the HTTP request pipeline
    if (app.Environment.IsDevelopment())
    {
        app.UseSwagger();
        app.UseSwaggerUI();
    }

    // In DEV we often run only HTTP (esp. mobile emulator), so avoid forcing HTTPS redirects.
    if (!app.Environment.IsDevelopment())
    {
        app.UseHttpsRedirection();
    }
    app.UseRouting();
    app.UseCors();
    app.UseLocalizationConfiguration();
    app.UseSession();
    app.UseMiddleware<BffCsrfMiddleware>();
    app.UseGlobalExceptionHandler(); // Captura todas as exceções (domínio e genéricas) e retorna mensagens amigáveis
    app.UseAuthentication();
    app.UseAuthorization();

    app.MapControllers();

    try
    {
        Log.Information("Aplicação iniciada com sucesso");
        app.Run();
    }
    catch (Exception ex)
    {
        Log.Fatal(ex, "Aplicação encerrada inesperadamente");
        throw;
    }
    finally
    {
        Log.CloseAndFlush();
    }
}
catch (Exception ex)
{
    Log.Fatal(ex, "Falha ao inicializar a aplicação");
    throw;
}
finally
{
    Log.CloseAndFlush();
}
