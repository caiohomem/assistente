using AssistenteExecutivo.Api.Resources;
using AssistenteExecutivo.Domain.Exceptions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Localization;
using Npgsql;
using Serilog.Context;
using System.Diagnostics;
using System.Net;
using System.Security.Cryptography;
using System.Text.Json;

namespace AssistenteExecutivo.Api.Middleware;

public class GlobalExceptionHandlingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<GlobalExceptionHandlingMiddleware> _logger;
    private readonly IStringLocalizer<Messages> _localizer;
    private readonly IWebHostEnvironment _environment;
    private readonly IConfiguration _configuration;

    public GlobalExceptionHandlingMiddleware(
        RequestDelegate next,
        ILogger<GlobalExceptionHandlingMiddleware> logger,
        IStringLocalizer<Messages> localizer,
        IWebHostEnvironment environment,
        IConfiguration configuration)
    {
        _next = next;
        _logger = logger;
        _localizer = localizer;
        _environment = environment;
        _configuration = configuration;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var stopwatch = Stopwatch.StartNew();
        var requestPath = context.Request.Path.Value ?? string.Empty;
        var requestMethod = context.Request.Method;
        var userName = context.User?.Identity?.Name ?? "Anonymous";

        // Enriquecer o contexto de log com informações da requisição
        using (LogContext.PushProperty("RequestPath", requestPath))
        using (LogContext.PushProperty("RequestMethod", requestMethod))
        using (LogContext.PushProperty("UserName", userName))
        using (LogContext.PushProperty("StatusCode", context.Response.StatusCode))
        {
            try
            {
                await _next(context);
                stopwatch.Stop();

                // Log informações da requisição bem-sucedida (apenas para erros ou warnings)
                // Reduzir log level para 404s em paths comuns (root, favicon, etc) para evitar ruído
                if (context.Response.StatusCode >= 400)
                {
                    using (LogContext.PushProperty("Elapsed", stopwatch.ElapsedMilliseconds))
                    {
                        // 404s em paths comuns são esperados e não precisam de warning
                        var isCommon404 = context.Response.StatusCode == 404 &&
                            (requestPath == "/" ||
                             requestPath.StartsWith("/favicon", StringComparison.OrdinalIgnoreCase) ||
                             requestPath.StartsWith("/robots.txt", StringComparison.OrdinalIgnoreCase) ||
                             // WordPress scanner probes (common bot scans)
                             requestPath.Contains("/wp-includes/", StringComparison.OrdinalIgnoreCase) ||
                             requestPath.Contains("/wp-admin/", StringComparison.OrdinalIgnoreCase) ||
                             requestPath.Contains("/wp-content/", StringComparison.OrdinalIgnoreCase) ||
                             requestPath.Contains("wlwmanifest.xml", StringComparison.OrdinalIgnoreCase) ||
                             requestPath.Contains("xmlrpc.php", StringComparison.OrdinalIgnoreCase) ||
                             requestPath.Contains("/.well-known/", StringComparison.OrdinalIgnoreCase));

                        if (isCommon404)
                        {
                            _logger.LogDebug(
                                "Requisição retornou status {StatusCode}. Path: {Path}, Method: {Method}, User: {User}, Elapsed: {Elapsed}ms",
                                context.Response.StatusCode,
                                requestPath,
                                requestMethod,
                                userName,
                                stopwatch.ElapsedMilliseconds);
                        }
                        else
                        {
                            _logger.LogWarning(
                                "Requisição retornou status {StatusCode}. Path: {Path}, Method: {Method}, User: {User}, Elapsed: {Elapsed}ms",
                                context.Response.StatusCode,
                                requestPath,
                                requestMethod,
                                userName,
                                stopwatch.ElapsedMilliseconds);
                        }
                    }
                }
            }
            catch (UnauthorizedAccessException ex)
            {
                stopwatch.Stop();
                await HandleUnauthorizedExceptionAsync(context, ex, requestPath, requestMethod, userName, stopwatch.ElapsedMilliseconds);
            }
            catch (DomainException ex)
            {
                stopwatch.Stop();
                await HandleDomainExceptionAsync(context, ex, requestPath, requestMethod, userName, stopwatch.ElapsedMilliseconds);
            }
            catch (DbUpdateException ex) when (ex.InnerException is PostgresException pgEx)
            {
                stopwatch.Stop();
                await HandleDatabaseExceptionAsync(context, ex, pgEx, requestPath, requestMethod, userName, stopwatch.ElapsedMilliseconds);
            }
            catch (CryptographicException ex) when (ex.Message.Contains("key") && ex.Message.Contains("not found"))
            {
                stopwatch.Stop();
                await HandleCryptographicExceptionAsync(context, ex, requestPath, requestMethod, userName, stopwatch.ElapsedMilliseconds);
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                await HandleGenericExceptionAsync(context, ex, requestPath, requestMethod, userName, stopwatch.ElapsedMilliseconds);
            }
        }
    }

    private async Task HandleUnauthorizedExceptionAsync(
        HttpContext context,
        UnauthorizedAccessException exception,
        string requestPath,
        string requestMethod,
        string userName,
        long elapsedMs)
    {
        // Log da exceção de não autorizado (Warning pois são erros de autenticação esperados)
        using (LogContext.PushProperty("Elapsed", elapsedMs))
        using (LogContext.PushProperty("ExceptionType", exception.GetType().FullName))
        {
            _logger.LogWarning(
                exception,
                "Usuário não autorizado. Path: {Path}, Method: {Method}, User: {User}, Elapsed: {Elapsed}ms, Message: {Message}",
                requestPath,
                requestMethod,
                userName,
                elapsedMs,
                exception.Message);
        }

        // Se a resposta já foi iniciada, não podemos modificá-la
        if (context.Response.HasStarted)
        {
            _logger.LogWarning("Não foi possível tratar UnauthorizedAccessException: resposta já foi iniciada");
            return;
        }

        // Verificar se é uma requisição de API (JSON) ou web (HTML)
        var acceptHeader = context.Request.Headers.Accept.ToString();
        var isApiRequest = acceptHeader.Contains("application/json", StringComparison.OrdinalIgnoreCase) ||
                          requestPath.StartsWith("/api/", StringComparison.OrdinalIgnoreCase);

        context.Response.StatusCode = (int)HttpStatusCode.Unauthorized;

        if (isApiRequest)
        {
            // Para requisições de API, retornar JSON
            context.Response.ContentType = "application/json";
            var response = new
            {
                message = exception.Message,
                error = "Unauthorized"
            };

            var json = JsonSerializer.Serialize(response, new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            });

            await context.Response.WriteAsync(json);
        }
        else
        {
            // Para requisições web, redirecionar para login
            var frontendBaseUrl = _configuration["Frontend:BaseUrl"]
                ?? throw new InvalidOperationException("Frontend:BaseUrl não configurado em appsettings");
            var loginUrl = $"{frontendBaseUrl.TrimEnd('/')}/login?returnUrl={Uri.EscapeDataString(requestPath)}";

            context.Response.Redirect(loginUrl);
        }
    }

    private async Task HandleDomainExceptionAsync(
        HttpContext context,
        DomainException exception,
        string requestPath,
        string requestMethod,
        string userName,
        long elapsedMs)
    {
        // Log da exceção de domínio (Warning pois são erros de negócio esperados)
        using (LogContext.PushProperty("Elapsed", elapsedMs))
        using (LogContext.PushProperty("ExceptionType", exception.GetType().FullName))
        using (LogContext.PushProperty("LocalizationCode", exception.LocalizationCode ?? string.Empty))
        {
            _logger.LogWarning(
                exception,
                "Exceção de domínio capturada. Path: {Path}, Method: {Method}, User: {User}, Elapsed: {Elapsed}ms, Message: {Message}",
                requestPath,
                requestMethod,
                userName,
                elapsedMs,
                exception.Message);
        }

        // Se a resposta já foi iniciada, não podemos modificá-la
        if (context.Response.HasStarted)
        {
            _logger.LogWarning("Não foi possível tratar DomainException: resposta já foi iniciada");
            return;
        }

        context.Response.ContentType = "application/json";

        // Determinar status HTTP baseado no código de localização
        // Exceções de "não encontrado" devem retornar 404
        var statusCode = HttpStatusCode.BadRequest;
        if (!string.IsNullOrEmpty(exception.LocalizationCode))
        {
            if (exception.LocalizationCode.Contains("NaoEncontrado", StringComparison.OrdinalIgnoreCase))
            {
                statusCode = HttpStatusCode.NotFound;
            }
        }

        context.Response.StatusCode = (int)statusCode;

        // Usar código de localização se disponível, senão usar mensagem original
        string localizedMessage;
        if (!string.IsNullOrEmpty(exception.LocalizationCode))
        {
            var parameters = exception.LocalizationParameters ?? Array.Empty<object>();

            // Converter "Domain:RelationshipJaExiste" para "Domain.RelationshipJaExiste"
            // para corresponder à estrutura do arquivo JSON
            var localizationKey = exception.LocalizationCode.Replace(":", ".");
            localizedMessage = _localizer[localizationKey, parameters];

            // Se a localização falhou (retornou o código), usar mensagem padrão
            if (string.IsNullOrEmpty(localizedMessage) || localizedMessage == localizationKey)
            {
                localizedMessage = exception.Message;
            }
        }
        else
        {
            localizedMessage = exception.Message;
        }

        var response = new
        {
            message = localizedMessage,
            error = exception.GetType().Name
        };

        var json = JsonSerializer.Serialize(response, new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        });

        await context.Response.WriteAsync(json);
    }

    private async Task HandleDatabaseExceptionAsync(
        HttpContext context,
        DbUpdateException exception,
        PostgresException pgEx,
        string requestPath,
        string requestMethod,
        string userName,
        long elapsedMs)
    {
        // Log detalhado da exceção de banco de dados
        using (LogContext.PushProperty("Elapsed", elapsedMs))
        using (LogContext.PushProperty("ExceptionType", exception.GetType().FullName))
        using (LogContext.PushProperty("PostgresErrorCode", pgEx.SqlState))
        using (LogContext.PushProperty("PostgresMessage", pgEx.Message))
        {
            _logger.LogError(
                exception,
                "Erro de banco de dados. Path: {Path}, Method: {Method}, User: {User}, Elapsed: {Elapsed}ms, PostgresErrorCode: {PostgresErrorCode}, Message: {Message}",
                requestPath,
                requestMethod,
                userName,
                elapsedMs,
                pgEx.SqlState,
                pgEx.Message);
        }

        // Se a resposta já foi iniciada, não podemos modificá-la
        if (context.Response.HasStarted)
        {
            _logger.LogWarning("Não foi possível tratar DbUpdateException: resposta já foi iniciada");
            return;
        }

        context.Response.ContentType = "application/json";
        context.Response.StatusCode = (int)HttpStatusCode.BadRequest;

        // Mensagem amigável baseada no código de erro do PostgreSQL
        string userMessage;
        if (pgEx.SqlState == "22001") // value too long for type
        {
            userMessage = "Os dados fornecidos excedem o tamanho máximo permitido. Por favor, reduza o tamanho e tente novamente.";
        }
        else if (pgEx.SqlState == "23505") // unique_violation
        {
            userMessage = "Já existe um registro com esses dados. Por favor, verifique e tente novamente.";
        }
        else if (pgEx.SqlState == "23503") // foreign_key_violation
        {
            userMessage = "Referência inválida. O registro referenciado não existe.";
        }
        else if (pgEx.SqlState == "23502") // not_null_violation
        {
            userMessage = "Campo obrigatório não foi preenchido. Por favor, verifique os dados e tente novamente.";
        }
        else
        {
            // Erro genérico de banco de dados
            if (_environment.IsDevelopment())
            {
                userMessage = $"Erro de banco de dados: {pgEx.Message} (Código: {pgEx.SqlState})";
            }
            else
            {
                userMessage = "Ocorreu um erro ao processar os dados. Por favor, tente novamente mais tarde.";
            }
        }

        var response = new
        {
            message = userMessage,
            error = "DatabaseError",
            errorCode = pgEx.SqlState
        };

        var json = JsonSerializer.Serialize(response, new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        });

        await context.Response.WriteAsync(json);
    }

    private async Task HandleCryptographicExceptionAsync(
        HttpContext context,
        CryptographicException exception,
        string requestPath,
        string requestMethod,
        string userName,
        long elapsedMs)
    {
        // Log detalhado da exceção de Data Protection
        using (LogContext.PushProperty("Elapsed", elapsedMs))
        using (LogContext.PushProperty("ExceptionType", exception.GetType().FullName))
        {
            _logger.LogWarning(
                exception,
                "Erro de Data Protection (chave não encontrada). Path: {Path}, Method: {Method}, User: {User}, Elapsed: {Elapsed}ms, Message: {Message}. " +
                "Isso geralmente ocorre quando a aplicação reinicia ou escala e as chaves não estão persistidas. " +
                "A sessão será invalidada e o usuário precisará fazer login novamente.",
                requestPath,
                requestMethod,
                userName,
                elapsedMs,
                exception.Message);
        }

        // Se a resposta já foi iniciada, não podemos modificá-la
        if (context.Response.HasStarted)
        {
            _logger.LogWarning("Não foi possível tratar CryptographicException: resposta já foi iniciada");
            return;
        }

        // Limpar o cookie de sessão inválido
        // Usar as mesmas configurações do cookie de sessão para garantir que seja deletado corretamente
        var cookieOptions = new CookieOptions
        {
            Path = "/",
            HttpOnly = true,
            Secure = context.Request.IsHttps,
            SameSite = SameSiteMode.Lax,
            Expires = DateTimeOffset.UtcNow.AddDays(-1) // Expirar o cookie
        };

        // Se houver um domínio configurado no cookie de sessão, usar o mesmo
        var apiPublicBaseUrl = _configuration["Api:PublicBaseUrl"] ?? _configuration["Api:BaseUrl"];
        if (!string.IsNullOrWhiteSpace(apiPublicBaseUrl) && Uri.TryCreate(apiPublicBaseUrl, UriKind.Absolute, out var apiUri))
        {
            var host = apiUri.Host;
            var parts = host.Split('.');
            if (parts.Length >= 2)
            {
                var domainBase = parts.Length >= 3 && parts[parts.Length - 2].Length <= 3
                    ? string.Join(".", parts.Skip(parts.Length - 3))
                    : string.Join(".", parts.Skip(parts.Length - 2));
                cookieOptions.Domain = $".{domainBase}";
            }
        }

        context.Response.Cookies.Delete("ae.sid", cookieOptions);

        // Verificar se é uma requisição de API (JSON) ou web (HTML)
        var acceptHeader = context.Request.Headers.Accept.ToString();
        var isApiRequest = acceptHeader.Contains("application/json", StringComparison.OrdinalIgnoreCase) ||
                          requestPath.StartsWith("/api/", StringComparison.OrdinalIgnoreCase);

        if (isApiRequest)
        {
            // Para requisições de API, retornar 401 Unauthorized
            context.Response.StatusCode = (int)HttpStatusCode.Unauthorized;
            context.Response.ContentType = "application/json";
            var response = new
            {
                message = "Sessão expirada ou inválida. Por favor, faça login novamente.",
                error = "SessionExpired"
            };

            var json = JsonSerializer.Serialize(response, new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            });

            await context.Response.WriteAsync(json);
        }
        else
        {
            // Para requisições web, redirecionar para login
            var frontendBaseUrl = _configuration["Frontend:BaseUrl"]
                ?? throw new InvalidOperationException("Frontend:BaseUrl não configurado em appsettings");
            var loginUrl = $"{frontendBaseUrl.TrimEnd('/')}/login?returnUrl={Uri.EscapeDataString(requestPath)}";

            context.Response.StatusCode = (int)HttpStatusCode.Redirect;
            context.Response.Headers.Location = loginUrl;
            await context.Response.WriteAsync(string.Empty);
        }
    }

    private async Task HandleGenericExceptionAsync(
        HttpContext context,
        Exception exception,
        string requestPath,
        string requestMethod,
        string userName,
        long elapsedMs)
    {
        // Log detalhado da exceção (Error pois são erros inesperados)
        using (LogContext.PushProperty("Elapsed", elapsedMs))
        using (LogContext.PushProperty("ExceptionType", exception.GetType().FullName))
        using (LogContext.PushProperty("StackTrace", exception.StackTrace ?? string.Empty))
        using (LogContext.PushProperty("InnerException", exception.InnerException?.ToString() ?? string.Empty))
        {
            _logger.LogError(
                exception,
                "Exceção não tratada capturada. Path: {Path}, Method: {Method}, User: {User}, Elapsed: {Elapsed}ms, ExceptionType: {ExceptionType}, Message: {Message}, StackTrace: {StackTrace}",
                requestPath,
                requestMethod,
                userName,
                elapsedMs,
                exception.GetType().FullName,
                exception.Message,
                exception.StackTrace ?? "N/A");

            // Log all inner exceptions recursively
            LogInnerExceptions(exception.InnerException, 1);
        }

        context.Response.ContentType = "application/json";
        context.Response.StatusCode = (int)HttpStatusCode.InternalServerError;

        // Mensagem amigável para o usuário
        string userMessage;
        if (_environment.IsDevelopment())
        {
            // Em desenvolvimento, mostrar detalhes da exceção
            userMessage = exception.Message;
        }
        else
        {
            // Em produção, mostrar mensagem genérica amigável
            userMessage = _localizer["Errors.InternalServerError"];
            if (string.IsNullOrEmpty(userMessage) || userMessage == "Errors.InternalServerError")
            {
                userMessage = "Ocorreu um erro inesperado. Por favor, tente novamente mais tarde.";
            }
        }

        var response = new
        {
            message = userMessage,
            error = "InternalServerError"
        };

        // Em desenvolvimento, adicionar detalhes adicionais
        if (_environment.IsDevelopment())
        {
            var devResponse = new
            {
                message = userMessage,
                error = "InternalServerError",
                exception = exception.GetType().Name,
                stackTrace = exception.StackTrace,
                innerException = exception.InnerException?.Message
            };

            var json = JsonSerializer.Serialize(devResponse, new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                WriteIndented = true
            });

            await context.Response.WriteAsync(json);
        }
        else
        {
            var json = JsonSerializer.Serialize(response, new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            });

            await context.Response.WriteAsync(json);
        }
    }

    /// <summary>
    /// Recursively logs all inner exceptions in the exception chain
    /// </summary>
    private void LogInnerExceptions(Exception? innerException, int depth)
    {
        if (innerException == null)
        {
            return;
        }

        _logger.LogError(
            innerException,
            "Inner Exception (Depth {Depth}): {InnerExceptionType}, Message: {InnerExceptionMessage}, StackTrace: {StackTrace}",
            depth,
            innerException.GetType().FullName,
            innerException.Message,
            innerException.StackTrace ?? "N/A");

        // Recursively log the next inner exception
        LogInnerExceptions(innerException.InnerException, depth + 1);
    }
}

public static class GlobalExceptionHandlingMiddlewareExtensions
{
    public static IApplicationBuilder UseGlobalExceptionHandler(this IApplicationBuilder builder)
    {
        return builder.UseMiddleware<GlobalExceptionHandlingMiddleware>();
    }
}

