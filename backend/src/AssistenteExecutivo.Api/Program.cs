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
using Microsoft.Extensions.Caching.StackExchangeRedis;
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

// Obter connection string
var connectionString = configuration.GetConnectionString("DefaultConnection")
    ?? throw new InvalidOperationException("ConnectionString 'DefaultConnection' não configurada");

// Detectar se é PostgreSQL ou SQL Server
var isPostgreSQL = connectionString.StartsWith("postgresql://", StringComparison.OrdinalIgnoreCase) ||
                   connectionString.StartsWith("postgres://", StringComparison.OrdinalIgnoreCase) ||
                   (connectionString.Contains("Host=", StringComparison.OrdinalIgnoreCase) && 
                    !connectionString.Contains("Trusted_Connection", StringComparison.OrdinalIgnoreCase));

var loggerConfig = new LoggerConfiguration()
    .MinimumLevel.Information()
    .MinimumLevel.Override("Microsoft", LogEventLevel.Warning)
    .MinimumLevel.Override("Microsoft.AspNetCore", LogEventLevel.Warning)
    .MinimumLevel.Override("Microsoft.EntityFrameworkCore", LogEventLevel.Warning)
    .Enrich.FromLogContext()
    .Enrich.WithMachineName()
    .Enrich.WithEnvironmentName()
    .Enrich.WithProperty("Environment", Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Production")
    .WriteTo.Console(
        outputTemplate: "[{Timestamp:HH:mm:ss} {Level:u3}] {Message:lj} {Properties:j}{NewLine}{Exception}");

// Configurar sink de banco de dados baseado no tipo
if (isPostgreSQL)
{
    // PostgreSQL - por enquanto usando apenas console
    // Nota: Para salvar logs no PostgreSQL, você pode:
    // 1. Usar um provider customizado
    // 2. Usar Serilog.Sinks.PostgreSQL (requer configuração adicional)
    // 3. Usar Serilog.Sinks.File e processar depois
    // Por enquanto, logs vão apenas para console quando usar PostgreSQL
    // A tabela Logs foi criada caso queira implementar um provider customizado
}
else
{
    // SQL Server - usar sink do SQL Server
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

    loggerConfig.WriteTo.MSSqlServer(
        connectionString: connectionString,
        sinkOptions: columnOptions,
        columnOptions: columnOptionsObj,
        restrictedToMinimumLevel: LogEventLevel.Warning); // Apenas Warning, Error e Fatal vão para o banco
}

Log.Logger = loggerConfig.CreateLogger();

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
        var keycloakBaseUrl = builder.Configuration["Keycloak:BaseUrl"] 
            ?? throw new InvalidOperationException("Keycloak:BaseUrl deve estar configurado em appsettings");
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

    // Session (para BFF) - Usando banco de dados para persistir sessões entre reinicializações
    var sessionConnectionString = builder.Configuration.GetConnectionString("DefaultConnection")
        ?? throw new InvalidOperationException("ConnectionString 'DefaultConnection' não configurada");
    
    // Detectar se é PostgreSQL ou SQL Server
    var isPostgreSQLSession = sessionConnectionString.Contains("Host=") || 
                              (sessionConnectionString.Contains("Server=") && sessionConnectionString.Contains("Database=") && !sessionConnectionString.Contains("Trusted_Connection"));
    
    if (isPostgreSQLSession)
    {
        // PostgreSQL - usar Redis ou Memory Cache como fallback
        // Nota: .NET não tem suporte nativo para PostgreSQL distributed cache
        // Usando Memory Cache como fallback (sessões serão perdidas ao reiniciar)
        // Para produção, considere usar Redis ou implementar um provider customizado
        // PostgreSQL - preferir Redis (recomendado) para suportar Cloud Run com mais de 1 instAcncia.
        // Sem Redis, a sessAćo pode cair em instAcncia diferente e perder OAuth state / tokens.
        var redisConnectionString =
            builder.Configuration.GetConnectionString("Redis")
            ?? builder.Configuration["Redis:ConnectionString"]
            ?? builder.Configuration["Redis:Configuration"];

        if (!string.IsNullOrWhiteSpace(redisConnectionString))
        {
            builder.Services.AddStackExchangeRedisCache(options =>
            {
                options.Configuration = redisConnectionString;
                options.InstanceName = "ae:";
            });
        }
        else
        {
            if (!builder.Environment.IsDevelopment())
            {
                Log.Warning("PostgreSQL detected for SessionCache but Redis is not configured. In Cloud Run with multiple instances this can cause OAuth invalid_state and BFF session loss.");
            }
            builder.Services.AddDistributedMemoryCache();
        }
    }
    else
    {
        // SQL Server
        builder.Services.AddDistributedSqlServerCache(options =>
        {
            options.ConnectionString = sessionConnectionString;
            options.SchemaName = "dbo";
            options.TableName = "SessionCache";
            // Expiração padrão de 20 minutos (as sessões individuais têm timeout de 30 minutos)
            options.DefaultSlidingExpiration = TimeSpan.FromMinutes(20);
        });
    }
    
    builder.Services.AddSession(options =>
    {
        options.IdleTimeout = TimeSpan.FromMinutes(30);
        options.Cookie.HttpOnly = true;
        options.Cookie.IsEssential = true;
        // NOTE: `SameSite=None` cookies must also be `Secure`. On HTTP localhost, the browser will drop the cookie,
        // which breaks the BFF session and can cause an auth redirect loop.
        var apiBaseUrl = builder.Configuration["Api:BaseUrl"];
        var isHttps =
            (!string.IsNullOrWhiteSpace(apiBaseUrl) && apiBaseUrl.StartsWith("https://", StringComparison.OrdinalIgnoreCase));
        options.Cookie.SameSite = isHttps ? SameSiteMode.None : SameSiteMode.Lax;
        options.Cookie.SecurePolicy = isHttps ? CookieSecurePolicy.Always : CookieSecurePolicy.SameAsRequest;
        options.Cookie.Name = "ae.sid";
        
        // Configurar Domain para funcionar entre subdomínios
        // Usar Api:PublicBaseUrl se disponível, senão usar Api:BaseUrl
        var apiPublicBaseUrl = builder.Configuration["Api:PublicBaseUrl"] ?? apiBaseUrl;
        if (!string.IsNullOrWhiteSpace(apiPublicBaseUrl)
            && Uri.TryCreate(apiPublicBaseUrl, UriKind.Absolute, out var apiUri)
            && !apiUri.Host.EndsWith(".run.app", StringComparison.OrdinalIgnoreCase))
        {
            // Extrair o domínio base (ex: assistente.live de api.assistente.live)
            var host = apiUri.Host;
            // Se for um subdomínio (ex: api.assistente.live), usar o domínio base
            var parts = host.Split('.');
            if (parts.Length >= 2)
            {
                // Pegar os últimos 2 ou 3 segmentos (ex: assistente.live ou exemplo.com.br)
                var domainBase = parts.Length >= 3 && parts[parts.Length - 2].Length <= 3 
                    ? string.Join(".", parts.Skip(parts.Length - 3)) // Para .com.br, .co.uk, etc
                    : string.Join(".", parts.Skip(parts.Length - 2)); // Para .com, .live, etc
                
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
            string[]? configuredOrigins = null;
            
            // Tentar ler como array primeiro (appsettings.json)
            var originsArray = builder.Configuration
                .GetSection("Frontend:CorsOrigins")
                .Get<string[]>();
            
            if (originsArray != null && originsArray.Length > 0)
            {
                configuredOrigins = originsArray;
            }
            else
            {
                // Tentar ler como string separada por vírgula (variáveis de ambiente)
                var originsString = builder.Configuration["Frontend:CorsOrigins"];
                if (!string.IsNullOrWhiteSpace(originsString))
                {
                    configuredOrigins = originsString
                        .Split(',', StringSplitOptions.RemoveEmptyEntries)
                        .Select(o => o.Trim())
                        .Where(o => !string.IsNullOrWhiteSpace(o))
                        .ToArray();
                }
            }

            // Back-compat: if Frontend:CorsOrigins não existir, usa BaseUrl.
            if (configuredOrigins == null || configuredOrigins.Length == 0)
            {
                var frontendBaseUrl = builder.Configuration["Frontend:BaseUrl"]
                    ?? throw new InvalidOperationException("Frontend:BaseUrl não configurado em appsettings");
                configuredOrigins = new[] { frontendBaseUrl.Trim().TrimEnd('/') };
            }

            // Limpar e normalizar URLs
            var finalOrigins = configuredOrigins
                .Select(o => o.Trim().TrimEnd('/'))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToArray();

            policy.WithOrigins(finalOrigins)
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
