using AssistenteExecutivo.Application.Json;
using AssistenteExecutivo.Api.Auth;
using AssistenteExecutivo.Api.Extensions;
using AssistenteExecutivo.Api.Middleware;
using AssistenteExecutivo.Api.Security;
using AssistenteExecutivo.Infrastructure;
using AssistenteExecutivo.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi;
using Serilog;
using Serilog.Events;
using StackExchange.Redis;
using System.IdentityModel.Tokens.Jwt;

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

// Logs são gravados apenas no console (em produção, usar Google Cloud Console)

Log.Logger = loggerConfig.CreateLogger();

try
{
    Log.Information("Iniciando aplicação AssistenteExecutivo.Api");

    // Helper function para obter connection string do Redis
    static string? GetRedisConnectionString(IConfiguration configuration)
    {
        // Prioridade 1: ConnectionStrings:Redis (formato StackExchange.Redis)
        var connectionString = configuration.GetConnectionString("Redis");
        if (!string.IsNullOrWhiteSpace(connectionString))
        {
            Log.Information("Redis configurado via ConnectionStrings:Redis");
            // Verificar se é uma URL que precisa ser convertida (caso alguém configure errado)
            if (connectionString.StartsWith("redis://", StringComparison.OrdinalIgnoreCase) ||
                connectionString.StartsWith("rediss://", StringComparison.OrdinalIgnoreCase))
            {
                Log.Warning("ConnectionStrings:Redis está no formato URL. Convertendo...");
                return ConvertRedisUrlToConnectionString(connectionString);
            }
            return connectionString;
        }

        // Prioridade 2: Redis:ConnectionString
        var redisConnectionString = configuration["Redis:ConnectionString"];
        if (!string.IsNullOrWhiteSpace(redisConnectionString))
        {
            Log.Information("Redis configurado via Redis:ConnectionString");
            // Verificar se é uma URL que precisa ser convertida
            if (redisConnectionString.StartsWith("redis://", StringComparison.OrdinalIgnoreCase) ||
                redisConnectionString.StartsWith("rediss://", StringComparison.OrdinalIgnoreCase))
            {
                Log.Warning("Redis:ConnectionString está no formato URL. Convertendo...");
                return ConvertRedisUrlToConnectionString(redisConnectionString);
            }
            return redisConnectionString;
        }

        // Prioridade 3: Redis:Configuration
        var redisConfiguration = configuration["Redis:Configuration"];
        if (!string.IsNullOrWhiteSpace(redisConfiguration))
        {
            Log.Information("Redis configurado via Redis:Configuration");
            // Verificar se é uma URL que precisa ser convertida
            if (redisConfiguration.StartsWith("redis://", StringComparison.OrdinalIgnoreCase) ||
                redisConfiguration.StartsWith("rediss://", StringComparison.OrdinalIgnoreCase))
            {
                Log.Warning("Redis:Configuration está no formato URL. Convertendo...");
                return ConvertRedisUrlToConnectionString(redisConfiguration);
            }
            return redisConfiguration;
        }

        Log.Information("Redis não configurado - usando fallback (SQL Server Cache ou Memory Cache)");
        return null;
    }

    // Helper function para converter URL Redis (rediss:// ou redis://) para formato StackExchange.Redis
    static string ConvertRedisUrlToConnectionString(string redisUrl)
    {
        if (string.IsNullOrWhiteSpace(redisUrl))
        {
            Log.Warning("Redis URL está vazia");
            return redisUrl;
        }

        // Se já está no formato correto (não começa com redis:// ou rediss://), retornar como está
        if (!redisUrl.StartsWith("redis://", StringComparison.OrdinalIgnoreCase) &&
            !redisUrl.StartsWith("rediss://", StringComparison.OrdinalIgnoreCase))
        {
            Log.Information("Redis connection string já está no formato correto (não é URL)");
            return redisUrl;
        }

        Log.Information("Convertendo Redis URL para formato StackExchange.Redis: {Url}",
            redisUrl.Contains("@") ? redisUrl.Substring(0, redisUrl.IndexOf("@")) + "@***" : redisUrl);

        try
        {
            // Parse da URL: rediss://user:password@host:port ou redis://user:password@host:port
            var uri = new Uri(redisUrl);
            var isSsl = uri.Scheme.Equals("rediss", StringComparison.OrdinalIgnoreCase);

            var host = uri.Host;
            if (string.IsNullOrWhiteSpace(host))
            {
                throw new ArgumentException("Host não pode ser vazio na URL do Redis", nameof(redisUrl));
            }

            // Porta: usar a porta da URL ou padrão baseado no esquema
            var port = uri.Port > 0 ? uri.Port : (isSsl ? 6380 : 6379);

            // Extrair user e password do UserInfo
            string? user = null;
            string? password = null;

            if (!string.IsNullOrWhiteSpace(uri.UserInfo))
            {
                var userInfoParts = uri.UserInfo.Split(':', 2);
                user = userInfoParts.Length > 0 && !string.IsNullOrWhiteSpace(userInfoParts[0])
                    ? userInfoParts[0]
                    : null;
                password = userInfoParts.Length > 1 && !string.IsNullOrWhiteSpace(userInfoParts[1])
                    ? userInfoParts[1]
                    : null;
            }

            // Construir connection string no formato StackExchange.Redis
            // Formato: host:port,ssl=true,password=...,user=...
            var parts = new List<string>();

            // Adicionar host:port (sempre primeiro) - CRÍTICO: não incluir o esquema
            parts.Add($"{host}:{port}");

            // Configurações SSL (para rediss://)
            if (isSsl)
            {
                parts.Add("ssl=true");
                parts.Add("abortConnect=false");
                // Upstash e outros serviços cloud geralmente precisam de timeout maior
                parts.Add("connectTimeout=15000");
                parts.Add("syncTimeout=15000");
                // Configurações adicionais para melhorar confiabilidade
                parts.Add("asyncTimeout=15000");
            }

            // Adicionar password (se existir)
            string? decodedPassword = null;
            if (!string.IsNullOrWhiteSpace(password))
            {
                // Decodificar password (pode ter caracteres especiais codificados)
                decodedPassword = Uri.UnescapeDataString(password);
                // Escapar caracteres especiais no password se necessário
                parts.Add($"password={decodedPassword}");
            }

            // Adicionar user (se existir e não for "default")
            if (!string.IsNullOrWhiteSpace(user) && !user.Equals("default", StringComparison.OrdinalIgnoreCase))
            {
                var decodedUser = Uri.UnescapeDataString(user);
                parts.Add($"user={decodedUser}");
            }

            var connectionString = string.Join(",", parts);

            // Validar que a conversão funcionou (não deve começar com redis:// ou rediss://)
            if (connectionString.StartsWith("redis://", StringComparison.OrdinalIgnoreCase) ||
                connectionString.StartsWith("rediss://", StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidOperationException(
                    $"Falha na conversão: connection string ainda começa com redis:// ou rediss://. " +
                    $"Valor: {connectionString.Substring(0, Math.Min(100, connectionString.Length))}");
            }

            // Log da conversão (sem expor senha)
            var logOriginal = redisUrl;
            if (!string.IsNullOrWhiteSpace(password))
            {
                logOriginal = redisUrl.Replace(password, "***");
            }
            var logConverted = connectionString;
            if (!string.IsNullOrWhiteSpace(decodedPassword))
            {
                logConverted = connectionString.Replace(decodedPassword, "***");
            }

            Log.Information("Redis URL convertida com sucesso: {OriginalUrl} -> {ConnectionString}",
                logOriginal, logConverted);

            return connectionString;
        }
        catch (Exception ex)
        {
            Log.Error(ex, "ERRO CRÍTICO ao converter REDIS_URL: {RedisUrl}. Exception: {ExceptionType}: {Message}",
                redisUrl.Contains("@") ? redisUrl.Substring(0, redisUrl.IndexOf("@")) + "@***" : redisUrl,
                ex.GetType().Name, ex.Message);
            // Não retornar a URL original - lançar exceção para forçar correção
            throw new InvalidOperationException(
                $"Não foi possível converter REDIS_URL para formato StackExchange.Redis. " +
                $"Configure usando o formato: host:port,password=...,ssl=true. " +
                $"Erro: {ex.Message}", ex);
        }
    }

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
    builder.Services.AddControllers()
        .AddJsonOptions(options =>
        {
            options.JsonSerializerOptions.PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase;
            options.JsonSerializerOptions.Converters.Add(new CaseInsensitiveJsonStringEnumConverter());
        });
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

    // Session (para BFF) - Usando Redis (obrigatório em produção)
    var redisConnectionString = GetRedisConnectionString(builder.Configuration);

    if (!string.IsNullOrWhiteSpace(redisConnectionString))
    {
        // CRÍTICO: Garantir que nunca passamos uma URL diretamente para StackExchange.Redis
        // StackExchange.Redis não aceita URLs no formato rediss:// diretamente
        if (redisConnectionString.StartsWith("redis://", StringComparison.OrdinalIgnoreCase) ||
            redisConnectionString.StartsWith("rediss://", StringComparison.OrdinalIgnoreCase))
        {
            Log.Warning("Redis connection string ainda está no formato URL. Convertendo forçadamente...");
            redisConnectionString = ConvertRedisUrlToConnectionString(redisConnectionString);

            // Validar que a conversão funcionou
            if (redisConnectionString.StartsWith("redis://", StringComparison.OrdinalIgnoreCase) ||
                redisConnectionString.StartsWith("rediss://", StringComparison.OrdinalIgnoreCase))
            {
                Log.Error("FALHA CRÍTICA: Conversão do Redis URL falhou! A connection string ainda está no formato URL.");
                throw new InvalidOperationException(
                    $"Redis connection string não pôde ser convertida do formato URL. " +
                    $"Valor recebido: {redisConnectionString.Substring(0, Math.Min(50, redisConnectionString.Length))}... " +
                    $"Configure usando o formato StackExchange.Redis: host:port,password=...,ssl=true");
            }
        }

        // Log da connection string final (sem expor senha)
        var logConnectionString = redisConnectionString;
        if (logConnectionString.Contains("password="))
        {
            var passwordIndex = logConnectionString.IndexOf("password=");
            var beforePassword = logConnectionString.Substring(0, passwordIndex);
            var afterPassword = logConnectionString.Substring(passwordIndex);
            var passwordEnd = afterPassword.IndexOf(",");
            if (passwordEnd > 0)
            {
                logConnectionString = beforePassword + "password=***" + afterPassword.Substring(passwordEnd);
            }
            else
            {
                logConnectionString = beforePassword + "password=***";
            }
        }

        Log.Information("Configurando Redis para session storage. ConnectionString format: {Format}, Length: {Length}",
            logConnectionString, redisConnectionString.Length);

        // Validar formato antes de configurar
        if (!redisConnectionString.Contains(":") || redisConnectionString.StartsWith("redis"))
        {
            Log.Error("Redis connection string em formato inválido: {ConnectionString}",
                redisConnectionString.Substring(0, Math.Min(100, redisConnectionString.Length)));
            throw new InvalidOperationException(
                "Redis connection string deve estar no formato: host:port,password=...,ssl=true");
        }

        builder.Services.AddStackExchangeRedisCache(options =>
        {
            options.Configuration = redisConnectionString;
            options.InstanceName = "ae:";
        });
    }
    else
    {
        // Redis não configurado - usar Memory Cache apenas em desenvolvimento
        if (!builder.Environment.IsDevelopment())
        {
            Log.Warning("Redis não configurado em ambiente de produção. Isso pode causar perda de sessões em múltiplas instâncias.");
        }
        builder.Services.AddDistributedMemoryCache();
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
            && Uri.TryCreate(apiPublicBaseUrl, UriKind.Absolute, out var apiUri))
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

    // Data Protection - Persist keys to Redis when available, otherwise use file system
    // This is critical for Cloud Run where instances restart and scale, losing in-memory keys
    if (!string.IsNullOrWhiteSpace(redisConnectionString))
    {
        // Use Redis for Data Protection keys (shared across all instances)
        Log.Information("Configurando Data Protection para usar Redis");
        try
        {
            // Create Redis connection for Data Protection
            var redis = ConnectionMultiplexer.Connect(redisConnectionString);
            builder.Services.AddDataProtection()
                .PersistKeysToStackExchangeRedis(redis, "DataProtection-Keys")
                .SetApplicationName("AssistenteExecutivo")
                .SetDefaultKeyLifetime(TimeSpan.FromDays(90)); // Keys valid for 90 days
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Erro ao configurar Data Protection com Redis. Usando fallback para file system.");
            // Fallback to file system if Redis connection fails
            var dataProtectionPath = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "AssistenteExecutivo",
                "DataProtection-Keys");
            Directory.CreateDirectory(dataProtectionPath);
            builder.Services.AddDataProtection()
                .PersistKeysToFileSystem(new DirectoryInfo(dataProtectionPath))
                .SetApplicationName("AssistenteExecutivo")
                .SetDefaultKeyLifetime(TimeSpan.FromDays(90));
        }
    }
    else
    {
        // Fallback: Use file system (ephemeral in Cloud Run, but better than in-memory)
        // In production, Redis should be configured to avoid key loss on restart/scale
        if (!builder.Environment.IsDevelopment())
        {
            Log.Warning("Redis não configurado para Data Protection. Chaves serão armazenadas em file system (ephemeral em Cloud Run). Configure Redis para produção.");
        }

        var dataProtectionPath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "AssistenteExecutivo",
            "DataProtection-Keys");
        Directory.CreateDirectory(dataProtectionPath);
        builder.Services.AddDataProtection()
            .PersistKeysToFileSystem(new DirectoryInfo(dataProtectionPath))
            .SetApplicationName("AssistenteExecutivo")
            .SetDefaultKeyLifetime(TimeSpan.FromDays(90));
    }

    // CORS
    builder.Services.AddCors(options =>
    {
        options.AddDefaultPolicy(policy =>
        {
            string[]? configuredOrigins = null;

            // PRIORIDADE 1: Tentar ler como string separada por vírgula (variáveis de ambiente)
            // Variáveis de ambiente têm prioridade sobre appsettings.json
            var originsString = builder.Configuration["Frontend:CorsOrigins"];
            if (!string.IsNullOrWhiteSpace(originsString))
            {
                configuredOrigins = originsString
                    .Split(',', StringSplitOptions.RemoveEmptyEntries)
                    .Select(o => o.Trim())
                    .Where(o => !string.IsNullOrWhiteSpace(o))
                    .ToArray();
            }

            // PRIORIDADE 2: Se não encontrou string, tentar ler como array (appsettings.json)
            if (configuredOrigins == null || configuredOrigins.Length == 0)
            {
                var originsArray = builder.Configuration
                    .GetSection("Frontend:CorsOrigins")
                    .Get<string[]>();

                if (originsArray != null && originsArray.Length > 0)
                {
                    configuredOrigins = originsArray;
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

    // Log CORS origins na inicialização
    var corsOrigins = builder.Configuration.GetSection("Frontend:CorsOrigins").Get<string[]>();
    var corsOriginsString = builder.Configuration["Frontend:CorsOrigins"];
    var frontendBaseUrl = builder.Configuration["Frontend:BaseUrl"];
    Log.Information("CORS - Origins Array: {OriginsArray}, Origins String: {OriginsString}, BaseUrl: {BaseUrl}",
        corsOrigins != null ? string.Join(", ", corsOrigins) : "null",
        corsOriginsString ?? "null",
        frontendBaseUrl ?? "null");

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
