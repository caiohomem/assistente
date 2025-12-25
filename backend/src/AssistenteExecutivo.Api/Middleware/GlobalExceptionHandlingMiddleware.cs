using AssistenteExecutivo.Domain.Exceptions;
using AssistenteExecutivo.Api.Resources;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Configuration;
using System;
using System.Net;
using System.Text.Json;
using System.Diagnostics;
using Serilog.Context;

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
                if (context.Response.StatusCode >= 400)
                {
                    using (LogContext.PushProperty("Elapsed", stopwatch.ElapsedMilliseconds))
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
            var frontendBaseUrl = _configuration["Frontend:BaseUrl"] ?? "http://localhost:3000";
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
        {
            _logger.LogError(
                exception,
                "Exceção não tratada capturada. Path: {Path}, Method: {Method}, User: {User}, Elapsed: {Elapsed}ms, ExceptionType: {ExceptionType}, Message: {Message}",
                requestPath,
                requestMethod,
                userName,
                elapsedMs,
                exception.GetType().FullName,
                exception.Message);
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
}

public static class GlobalExceptionHandlingMiddlewareExtensions
{
    public static IApplicationBuilder UseGlobalExceptionHandler(this IApplicationBuilder builder)
    {
        return builder.UseMiddleware<GlobalExceptionHandlingMiddleware>();
    }
}

