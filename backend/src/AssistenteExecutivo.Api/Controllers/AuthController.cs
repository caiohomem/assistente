using System.IO;
using System.Linq;
using System.Net;
using System.Security.Cryptography;
using AssistenteExecutivo.Api.Security;
using AssistenteExecutivo.Application.Commands.Auth;
using AssistenteExecutivo.Application.Interfaces;
using AssistenteExecutivo.Application.Queries.Auth;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;

namespace AssistenteExecutivo.Api.Controllers;

[ApiController]
[Route("auth")]
public sealed class AuthController : ControllerBase
{
    private readonly IKeycloakService _keycloakService;
    private readonly IEmailService _emailService;
    private readonly IConfiguration _configuration;
    private readonly ILogger<AuthController> _logger;
    private readonly IMediator _mediator;

    public AuthController(
        IKeycloakService keycloakService,
        IEmailService emailService,
        IConfiguration configuration,
        ILogger<AuthController> logger,
        IMediator mediator)
    {
        _keycloakService = keycloakService;
        _emailService = emailService;
        _configuration = configuration;
        _logger = logger;
        _mediator = mediator;
    }

    public sealed record ForgotPasswordRequest(string Email);
    public sealed record ResetPasswordRequest(string Email, string Token, string NewPassword);

    /// <summary>
    /// Registra um novo usuário no sistema.
    /// </summary>
    [HttpPost("register")]
    public async Task<ActionResult<RegisterUserResult>> Register([FromBody] RegisterUserCommand command)
    {
        if (command == null)
            return BadRequest(new { message = "Requisição inválida." });

        if (string.IsNullOrWhiteSpace(command.Email))
            return BadRequest(new { message = "Email é obrigatório." });

        if (string.IsNullOrWhiteSpace(command.Password))
            return BadRequest(new { message = "Senha é obrigatória." });

        if (command.Password.Length < 8)
            return BadRequest(new { message = "Senha deve ter no mínimo 8 caracteres." });

        if (string.IsNullOrWhiteSpace(command.FirstName))
            return BadRequest(new { message = "Primeiro nome é obrigatório." });

        var result = await _mediator.Send(command);
        return Ok(result);
    }

    /// <summary>
    /// Starts login flow by redirecting to Keycloak authorize endpoint.
    /// </summary>
    [HttpGet("login")]
    public async Task<IActionResult> Login([FromQuery] string? provider = null, [FromQuery] string? returnUrl = null)
    {
        var realm = GetRealm();
        StoreFrontendBaseUrlForRedirect();
        var redirectUri = GetCallbackRedirectUri();

        var state = GenerateState();
        HttpContext.Session.SetString(BffSessionKeys.OAuthState, state);
        HttpContext.Session.SetString(BffSessionKeys.ReturnPath, NormalizeReturnPath(returnUrl));
        
        // IMPORTANTE: Commit explícito da sessão antes do redirect
        // Isso garante que o state seja persistido antes de redirecionar para o Keycloak
        await HttpContext.Session.CommitAsync(HttpContext.RequestAborted);
        
        // Log para debug - verificar se sessão foi salva
        var sessionIdAfterCommit = HttpContext.Session.Id;
        var stateStored = HttpContext.Session.GetString(BffSessionKeys.OAuthState);
        var hasSessionCookie = Response.Headers.ContainsKey("Set-Cookie");
        _logger.LogInformation(
            "Login - State gerado e armazenado. State: {State}, SessionId: {SessionId}, StateStored: {StateStored}, HasSetCookie: {HasSetCookie}",
            state, sessionIdAfterCommit, stateStored != null ? "OK" : "NULL", hasSessionCookie);

        string loginUrl;

        if (!string.IsNullOrWhiteSpace(provider))
        {
            loginUrl = await _keycloakService.GetSocialLoginUrlAsync(realm, provider, redirectUri, state);
            loginUrl = $"{loginUrl}&kc_action=register";
        }
        else
        {
            loginUrl = BuildDefaultAuthorizeUrl(realm, redirectUri, state);
        }

        return Redirect(loginUrl);
    }

    /// <summary>
    /// Starts registration flow by redirecting to Keycloak registration endpoint.
    /// </summary>
    [HttpGet("register")]
    public async Task<IActionResult> Register([FromQuery] string? provider = null, [FromQuery] string? returnUrl = null)
    {
        var realm = GetRealm();
        StoreFrontendBaseUrlForRedirect();
        var redirectUri = GetCallbackRedirectUri();

        var state = GenerateState();
        HttpContext.Session.SetString(BffSessionKeys.OAuthState, state);
        HttpContext.Session.SetString(BffSessionKeys.ReturnPath, NormalizeReturnPath(returnUrl));
        HttpContext.Session.SetString(BffSessionKeys.Action, "register"); // Marcar como registro
        
        // IMPORTANTE: Commit explícito da sessão antes do redirect
        // Isso garante que o state seja persistido antes de redirecionar para o Keycloak
        await HttpContext.Session.CommitAsync(HttpContext.RequestAborted);

        string loginUrl;

        if (!string.IsNullOrWhiteSpace(provider))
        {
            loginUrl = await _keycloakService.GetSocialLoginUrlAsync(realm, provider, redirectUri, state);
        }
        else
        {
            // Para registro, usar rota de autorização com parâmetros para forçar página de registro
            loginUrl = BuildDefaultAuthorizeUrl(realm, redirectUri, state);
            // Adicionar parâmetros para forçar página de registro
            // screen_hint=signup é um parâmetro OAuth2 padrão suportado pelo Keycloak
            var separator = loginUrl.Contains('?', StringComparison.Ordinal) ? "&" : "?";
            loginUrl = $"{loginUrl}{separator}kc_action=register";
            // Também tentar kc_action=REGISTER (específico do Keycloak)
        }

        return Redirect(loginUrl);
    }

    /// <summary>
    /// OAuth2 redirect URI handler. Exchanges code for tokens and stores session.
    /// </summary>
    [HttpGet("oauth-callback")]
    public async Task<IActionResult> OAuthCallback(
        [FromQuery] string? code = null,
        [FromQuery] string? state = null,
        [FromQuery] string? error = null,
        [FromQuery(Name = "error_description")] string? errorDescription = null)
    {
        var realm = GetRealm();

        if (!string.IsNullOrWhiteSpace(error))
        {
            _logger.LogWarning("OAuth callback error: {Error} {ErrorDescription}", error, errorDescription);
            return Redirect(BuildFrontendLoginUrlWithError(error));
        }

        if (string.IsNullOrWhiteSpace(code) || string.IsNullOrWhiteSpace(state))
            return BadRequest(new { error = "invalid_callback", message = "Parâmetros 'code' e 'state' são obrigatórios." });

        // Verificar se já está autenticado - se sim, redirecionar diretamente para evitar processamento duplicado
        if (BffSessionStore.IsAuthenticated(HttpContext.Session))
        {
            _logger.LogInformation("Callback OAuth recebido mas usuário já está autenticado. Redirecionando para dashboard.");
            return Redirect(BuildFrontendRedirectUrl());
        }

        // Debug: Verificar cookies e sessão
        var cookieHeader = Request.Headers["Cookie"].ToString();
        var hasSessionCookie = Request.Cookies.ContainsKey("ae.sid");
        var sessionId = HttpContext.Session?.Id ?? "null";
        var setCookieHeaders = Response.Headers["Set-Cookie"].ToString();
        
        _logger.LogInformation(
            "OAuthCallback - SessionId: {SessionId}, HasSessionCookie: {HasSessionCookie}, CookieHeaderLength: {CookieHeaderLength}, SetCookieHeadersLength: {SetCookieHeadersLength}",
            sessionId, hasSessionCookie, cookieHeader?.Length ?? 0, setCookieHeaders?.Length ?? 0);
        
        var expectedState = HttpContext.Session.GetString(BffSessionKeys.OAuthState);
        if (string.IsNullOrWhiteSpace(expectedState) || !FixedTimeEquals(expectedState, state))
        {
            _logger.LogWarning(
                "State inválido ou não encontrado. Expected: {ExpectedState}, Received: {ReceivedState}, SessionId: {SessionId}, HasSessionCookie: {HasSessionCookie}, CookieHeader: {CookieHeader}",
                expectedState ?? "null", state ?? "null", sessionId, hasSessionCookie, 
                cookieHeader?.Substring(0, Math.Min(200, cookieHeader?.Length ?? 0)) ?? "empty");
            return BadRequest(new { error = "invalid_state", message = "State inválido." });
        }

        // Verificar se este código já foi processado (proteção contra processamento duplicado)
        var processedCodeKey = $"oauth_code_{code}";
        var processedCode = HttpContext.Session.GetString(processedCodeKey);
        if (!string.IsNullOrWhiteSpace(processedCode))
        {
            _logger.LogWarning("Código OAuth já foi processado anteriormente. Code: {Code}", code?.Substring(0, Math.Min(10, code?.Length ?? 0)));
            // Se já foi processado e está autenticado, redirecionar
            if (BffSessionStore.IsAuthenticated(HttpContext.Session))
            {
                return Redirect(BuildFrontendRedirectUrl());
            }
            // Se não está autenticado, pode ser um código inválido ou expirado
            return Redirect(BuildFrontendLoginUrlWithError("oauth_code_already_used"));
        }

        // Marcar código como processado ANTES de processar (proteção contra race condition)
        HttpContext.Session.SetString(processedCodeKey, DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString());
        await HttpContext.Session.CommitAsync(HttpContext.RequestAborted);

        var redirectUri = GetCallbackRedirectUri();

        KeycloakTokenResult tokens;
        KeycloakUserInfo userInfo;
        try
        {
            tokens = await _keycloakService.ExchangeAuthorizationCodeAsync(realm, code, redirectUri, HttpContext.RequestAborted);
            userInfo = await _keycloakService.GetUserInfoAsync(realm, tokens.AccessToken, HttpContext.RequestAborted);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Falha ao concluir OAuth callback (token/userinfo) no realm {RealmId}", realm);
            return Redirect(BuildFrontendLoginUrlWithError("kc_oauth_callback_failed"));
        }

        // Verificar se o usuário existe no banco de dados
        if (string.IsNullOrWhiteSpace(userInfo.Email))
        {
            _logger.LogWarning("Email vazio retornado do Keycloak. UserInfo: Sub={Sub}, Name={Name}", userInfo.Sub, userInfo.Name);
            return Redirect(BuildFrontendLoginUrlWithError("email_nao_disponivel"));
        }

        // Verificar ação (login ou registro)
        var action = HttpContext.Session.GetString(BffSessionKeys.Action) ?? "login";
        var isRegister = action == "register";

        // Verificar se o usuário já existe no sistema ANTES de provisionar
        var checkUserQuery = new GetOwnerUserIdQuery
        {
            KeycloakSubject = userInfo.Sub
        };
        var existingUserId = await _mediator.Send(checkUserQuery, HttpContext.RequestAborted);

        // Declarar provisionResult fora dos blocos para uso posterior
        ProvisionUserFromKeycloakResult? provisionResult = null;

        if (isRegister)
        {
            // Se for registro e usuário já existe, retornar erro
            if (existingUserId != null)
            {
                _logger.LogWarning("Tentativa de registro com email já cadastrado. KeycloakSubject={KeycloakSubject}, Email={Email}", 
                    userInfo.Sub, userInfo.Email);
                HttpContext.Session.Remove(BffSessionKeys.Action);
                return Redirect(BuildFrontendLoginUrlWithError("email_ja_cadastrado"));
            }

            // Criar novo usuário (provisionamento)
            var provisionCommand = new ProvisionUserFromKeycloakCommand
            {
                KeycloakSubject = userInfo.Sub,
                Email = userInfo.Email,
                FirstName = userInfo.GivenName,
                LastName = userInfo.FamilyName,
                FullName = userInfo.Name
            };

            try
            {
                provisionResult = await _mediator.Send(provisionCommand, HttpContext.RequestAborted);
            }
            catch (AssistenteExecutivo.Domain.Exceptions.DomainException ex)
            {
                _logger.LogWarning(ex, "Provisionamento bloqueado durante registro. Email={Email}, Sub={Sub}", userInfo.Email, userInfo.Sub);
                HttpContext.Session.Clear();
                DeleteBffCookies();

                var authError = ex.LocalizationCode switch
                {
                    "Domain:UsuarioDeletado" => "usuario_deletado",
                    "Domain:UsuarioSuspenso" => "usuario_suspenso",
                    _ => "usuario_nao_permitido"
                };
                return Redirect(BuildFrontendLoginUrlWithError(authError));
            }

            _logger.LogInformation("Usuário registrado com sucesso. Email={Email}, UserId={UserId}, WasCreated={WasCreated}",
                userInfo.Email, provisionResult.UserId, provisionResult.WasCreated);
        }
        else
        {
            // Se for login e usuário não existe no banco, criar automaticamente (auto-provisionamento)
            // Isso acontece quando o usuário faz login social pela primeira vez
            if (existingUserId == null)
            {
                _logger.LogInformation("Usuário autenticado no Keycloak mas não encontrado no banco. Criando automaticamente (auto-provisionamento). KeycloakSubject={KeycloakSubject}, Email={Email}", 
                    userInfo.Sub, userInfo.Email);
            }

            // Provisionar usuário do Keycloak (criar se não existir, ou atualizar dados se necessário)
            var provisionCommand = new ProvisionUserFromKeycloakCommand
            {
                KeycloakSubject = userInfo.Sub,
                Email = userInfo.Email,
                FirstName = userInfo.GivenName,
                LastName = userInfo.FamilyName,
                FullName = userInfo.Name
            };

            try
            {
                provisionResult = await _mediator.Send(provisionCommand, HttpContext.RequestAborted);
            }
            catch (AssistenteExecutivo.Domain.Exceptions.DomainException ex)
            {
                _logger.LogWarning(ex, "Provisionamento bloqueado durante login. Email={Email}, Sub={Sub}", userInfo.Email, userInfo.Sub);
                HttpContext.Session.Clear();
                DeleteBffCookies();

                var authError = ex.LocalizationCode switch
                {
                    "Domain:UsuarioDeletado" => "usuario_deletado",
                    "Domain:UsuarioSuspenso" => "usuario_suspenso",
                    _ => "usuario_nao_permitido"
                };
                return Redirect(BuildFrontendLoginUrlWithError(authError));
            }

            if (provisionResult.WasCreated)
            {
                _logger.LogInformation("Usuário criado automaticamente via auto-provisionamento. Email={Email}, UserId={UserId}",
                    userInfo.Email, provisionResult.UserId);
            }
            else
            {
                _logger.LogInformation("Usuário {Email} autenticado com sucesso (UserId={UserId})",
                    userInfo.Email, provisionResult.UserId);
            }
        }

        // #region agent log
        var logPath = Path.Combine(Directory.GetCurrentDirectory(), ".cursor", "debug.log");
        var sessionIdBefore = HttpContext.Session.Id;
        var isAuthBefore = BffSessionStore.IsAuthenticated(HttpContext.Session);
        try { System.IO.File.AppendAllText(logPath, System.Text.Json.JsonSerializer.Serialize(new { id = $"log_{DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()}_A", timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(), location = "AuthController.OAuthCallback:281", message = "Before storing session", data = new { sessionId = sessionIdBefore, isAuthenticated = isAuthBefore, hasSession = HttpContext.Session != null, userId = provisionResult?.UserId ?? existingUserId ?? Guid.Empty }, sessionId = "debug-session", runId = "run1", hypothesisId = "A" }) + "\n"); } catch { }
        // #endregion

        var ownerUserId = provisionResult?.UserId ?? existingUserId;
        if (ownerUserId.HasValue && ownerUserId.Value != Guid.Empty)
        {
            BffSessionStore.StoreOwnerUserId(HttpContext.Session, ownerUserId.Value);
        }

        BffSessionStore.StoreTokens(HttpContext.Session, tokens);
        BffSessionStore.StoreUser(HttpContext.Session, userInfo);
        BffSessionStore.SetAuthenticated(HttpContext.Session, true);

        // #region agent log
        var isAuthAfterStore = BffSessionStore.IsAuthenticated(HttpContext.Session);
        try { System.IO.File.AppendAllText(logPath, System.Text.Json.JsonSerializer.Serialize(new { id = $"log_{DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()}_A", timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(), location = "AuthController.OAuthCallback:286", message = "After storing session data", data = new { sessionId = HttpContext.Session.Id, isAuthenticated = isAuthAfterStore, hasAccessToken = !string.IsNullOrEmpty(HttpContext.Session.GetString(BffSessionKeys.AccessToken)), hasUserEmail = !string.IsNullOrEmpty(HttpContext.Session.GetString(BffSessionKeys.UserEmail)) }, sessionId = "debug-session", runId = "run1", hypothesisId = "A" }) + "\n"); } catch { }
        // #endregion

        // Rotate CSRF token after login.
        BffCsrf.RotateToken(HttpContext.Session);
        SetCsrfCookie(BffCsrf.EnsureToken(HttpContext.Session));

        // Cleanup one-time values.
        HttpContext.Session.Remove(BffSessionKeys.OAuthState);
        HttpContext.Session.Remove(BffSessionKeys.Action);
        
        // Limpar código processado (já foi usado) - usar a mesma chave definida anteriormente
        HttpContext.Session.Remove(processedCodeKey);

        // CRÍTICO: Salvar a sessão explicitamente antes do redirect
        // Isso garante que todas as mudanças sejam persistidas antes de redirecionar
        await HttpContext.Session.CommitAsync(HttpContext.RequestAborted);

        // #region agent log
        var isAuthAfterCommit = BffSessionStore.IsAuthenticated(HttpContext.Session);
        var sessionIdAfterCommit = HttpContext.Session.Id;
        var cookieHeaderAfterCommit = Request.Headers["Cookie"].ToString();
        var setCookieHeadersAfterCommit = Response.Headers["Set-Cookie"].ToString();
        var hasAeSidInSetCookie = setCookieHeadersAfterCommit.Contains("ae.sid", StringComparison.OrdinalIgnoreCase);
        var hasAeSidInRequestCookie = Request.Cookies.ContainsKey("ae.sid");
        try { System.IO.File.AppendAllText(logPath, System.Text.Json.JsonSerializer.Serialize(new { id = $"log_{DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()}_B", timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(), location = "AuthController.OAuthCallback:298", message = "After CommitAsync", data = new { sessionId = sessionIdAfterCommit, isAuthenticated = isAuthAfterCommit, hasSetCookieHeader = !string.IsNullOrEmpty(setCookieHeadersAfterCommit), hasAeSidInSetCookie = hasAeSidInSetCookie, hasAeSidInRequestCookie = hasAeSidInRequestCookie, setCookieHeaderLength = setCookieHeadersAfterCommit?.Length ?? 0, setCookieHeader = setCookieHeadersAfterCommit?.Substring(0, Math.Min(500, setCookieHeadersAfterCommit?.Length ?? 0)), cookieHeaderLength = cookieHeaderAfterCommit?.Length ?? 0, requestHost = Request.Host.ToString(), requestScheme = Request.Scheme }, sessionId = "debug-session", runId = "run1", hypothesisId = "B" }) + "\n"); } catch { }
        // #endregion

        var redirectUrl = BuildFrontendRedirectUrl();
        
        // Verificar se a sessão foi realmente salva antes de redirecionar
        var sessionCheck = BffSessionStore.IsAuthenticated(HttpContext.Session);
        var sessionIdFinal = HttpContext.Session.Id;
        
        // provisionResult sempre será definido (tanto no bloco if quanto no else)
        var userId = provisionResult?.UserId ?? existingUserId ?? Guid.Empty;
        _logger.LogInformation(
            "Sessão autenticada salva. UserId={UserId}, Email={Email}, Sub={Sub}, SessionId={SessionId}, IsAuthenticated={IsAuthenticated}. Redirecionando para: {RedirectUrl}",
            userId, userInfo.Email, userInfo.Sub, sessionIdFinal, sessionCheck, redirectUrl);

        // #region agent log
        try { System.IO.File.AppendAllText(logPath, System.Text.Json.JsonSerializer.Serialize(new { id = $"log_{DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()}_A", timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(), location = "AuthController.OAuthCallback:316", message = "Before redirect", data = new { sessionId = sessionId, isAuthenticated = sessionCheck, redirectUrl = redirectUrl, userId = userId, email = userInfo.Email }, sessionId = "debug-session", runId = "run1", hypothesisId = "A" }) + "\n"); } catch { }
        // #endregion

        // Adicionar um pequeno delay para garantir que a sessão foi persistida
        // Isso ajuda quando há problemas de timing com cookies
        await Task.Delay(100, HttpContext.RequestAborted);

        return Redirect(redirectUrl);
    }

    /// <summary>
    /// Returns current BFF session state and a CSRF token for SPA calls.
    /// </summary>
    [HttpGet("session")]
    public async Task<IActionResult> Session()
    {
        // #region agent log
        var logPath = Path.Combine(Directory.GetCurrentDirectory(), ".cursor", "debug.log");
        var cookieHeader = Request.Headers["Cookie"].ToString();
        var sessionId = HttpContext.Session?.Id ?? "null";
        
        // Verificar cookie de forma mais robusta - verificar tanto no header quanto nos cookies do request
        var hasSessionCookieInHeader = cookieHeader.Contains("ae.sid", StringComparison.OrdinalIgnoreCase);
        var hasSessionCookieInRequest = Request.Cookies.ContainsKey("ae.sid");
        var hasSessionCookie = hasSessionCookieInHeader || hasSessionCookieInRequest;
        
        try { System.IO.File.AppendAllText(logPath, System.Text.Json.JsonSerializer.Serialize(new { id = $"log_{DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()}_C", timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(), location = "AuthController.Session:323", message = "Session endpoint called", data = new { sessionId = sessionId, hasSessionCookie = hasSessionCookie, hasSessionCookieInHeader = hasSessionCookieInHeader, hasSessionCookieInRequest = hasSessionCookieInRequest, cookieHeaderLength = cookieHeader?.Length ?? 0, cookieHeader = cookieHeader?.Substring(0, Math.Min(200, cookieHeader?.Length ?? 0)), requestHost = Request.Host.ToString(), requestScheme = Request.Scheme, userAgent = Request.Headers["User-Agent"].ToString() }, sessionId = "debug-session", runId = "run1", hypothesisId = "C" }) + "\n"); } catch { }
        // #endregion

        var realm = GetRealm();

        // Ensure CSRF token exists even for anonymous sessions (useful for login/logout buttons).
        var csrf = BffCsrf.EnsureToken(HttpContext.Session);
        SetCsrfCookie(csrf);

        // CRÍTICO: Se não há cookie de sessão, não pode estar autenticado
        // O ASP.NET Core pode criar uma nova sessão vazia quando HttpContext.Session é acessado,
        // mas sem o cookie, não há sessão persistida válida
        if (!hasSessionCookie)
        {
            // #region agent log
            try { System.IO.File.AppendAllText(logPath, System.Text.Json.JsonSerializer.Serialize(new { id = $"log_{DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()}_E", timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(), location = "AuthController.Session:noCookie", message = "No session cookie, returning unauthenticated", data = new { sessionId = sessionId, hasSessionCookie = false, hasSessionCookieInHeader = hasSessionCookieInHeader, hasSessionCookieInRequest = hasSessionCookieInRequest, cookieHeader = cookieHeader?.Substring(0, Math.Min(200, cookieHeader?.Length ?? 0)) }, sessionId = "debug-session", runId = "run1", hypothesisId = "E" }) + "\n"); } catch { }
            // #endregion
            
            return Ok(new
            {
                authenticated = false,
                csrfToken = csrf
            });
        }

        var isAuthenticated = BffSessionStore.IsAuthenticated(HttpContext.Session);
        var hasAccessToken = !string.IsNullOrEmpty(HttpContext.Session.GetString(BffSessionKeys.AccessToken));
        var hasUserEmail = !string.IsNullOrEmpty(HttpContext.Session.GetString(BffSessionKeys.UserEmail));

        // #region agent log
        try { System.IO.File.AppendAllText(logPath, System.Text.Json.JsonSerializer.Serialize(new { id = $"log_{DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()}_E", timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(), location = "AuthController.Session:332", message = "Session check result", data = new { sessionId = sessionId, isAuthenticated = isAuthenticated, hasAccessToken = hasAccessToken, hasUserEmail = hasUserEmail, userEmail = HttpContext.Session.GetString(BffSessionKeys.UserEmail) }, sessionId = "debug-session", runId = "run1", hypothesisId = "E" }) + "\n"); } catch { }
        // #endregion

        if (!isAuthenticated)
        {
            return Ok(new
            {
                authenticated = false,
                csrfToken = csrf
            });
        }

        // Auto-refresh access token if close to expiration.
        // Aumentado para 3 minutos (180 segundos) para reduzir refresh automático frequente
        // que pode causar perda de dados em formulários
        var expiresAtUnix = BffSessionStore.GetExpiresAtUnix(HttpContext.Session);
        var refreshToken = HttpContext.Session.GetString(BffSessionKeys.RefreshToken);
        var shouldRefresh = false;
        var tokenExpired = false;

        if (expiresAtUnix is not null && !string.IsNullOrWhiteSpace(refreshToken))
        {
            var now = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
            
            // Verificar se o token já expirou completamente
            if (expiresAtUnix.Value <= now)
            {
                // Token já expirou - tentar refresh mesmo assim
                tokenExpired = true;
                shouldRefresh = true;
                _logger.LogInformation("Access token já expirou. Tentando refresh. ExpiresAt: {ExpiresAt}, Now: {Now}", 
                    expiresAtUnix.Value, now);
            }
            // Verificar se o token expira em menos de 3 minutos (180 segundos)
            // Isso reduz a frequência de refresh automático e evita perda de dados em formulários
            else if (expiresAtUnix.Value - now <= 180)
            {
                shouldRefresh = true;
                _logger.LogInformation("Access token expira em breve. Fazendo refresh preventivo. ExpiresAt: {ExpiresAt}, Now: {Now}, SecondsUntilExpiry: {SecondsUntilExpiry}", 
                    expiresAtUnix.Value, now, expiresAtUnix.Value - now);
            }
        }

        // Tentar fazer refresh do token se necessário
        if (shouldRefresh)
        {
            try
            {
                var refreshed = await _keycloakService.RefreshTokenAsync(realm, refreshToken!, HttpContext.RequestAborted);
                BffSessionStore.StoreTokens(HttpContext.Session, refreshed);
                _logger.LogInformation("Token renovado com sucesso. Novo expiresAt: {ExpiresAt}", 
                    BffSessionStore.GetExpiresAtUnix(HttpContext.Session));
            }
            catch (HttpRequestException ex) when (
                ex.Message.Contains("400") || 
                ex.Message.Contains("Bad Request") ||
                ex.Message.Contains("401") ||
                ex.Message.Contains("Unauthorized") ||
                ex.Message.Contains("invalid_grant") ||
                ex.Message.Contains("invalid_token"))
            {
                // Refresh token expirado ou inválido - limpar sessão e retornar não autenticado
                _logger.LogWarning("Refresh token inválido ou expirado. Limpando sessão. Error: {Error}", ex.Message);
                HttpContext.Session.Clear();
                DeleteBffCookies();
                
                return Ok(new
                {
                    authenticated = false,
                    csrfToken = csrf
                });
            }
            catch (Exception ex)
            {
                // Outros erros ao renovar token - logar detalhadamente
                _logger.LogError(ex, "Erro inesperado ao renovar token. Error: {Error}", ex.Message);
                
                // Se o token já expirou completamente e o refresh falhou, limpar sessão
                if (tokenExpired)
                {
                    _logger.LogWarning("Token expirado e refresh falhou. Limpando sessão.");
                    HttpContext.Session.Clear();
                    DeleteBffCookies();
                    
                    return Ok(new
                    {
                        authenticated = false,
                        csrfToken = csrf
                    });
                }
                
                // Se o token ainda é válido, continuar autenticado mas logar o erro
                // O próximo request tentará refresh novamente
                _logger.LogWarning("Erro ao renovar token, mas token atual ainda pode ser válido. Continuando autenticado.");
            }
        }

        return Ok(new
        {
            authenticated = true,
            user = new
            {
                sub = HttpContext.Session.GetString(BffSessionKeys.UserSub),
                email = HttpContext.Session.GetString(BffSessionKeys.UserEmail),
                name = HttpContext.Session.GetString(BffSessionKeys.UserName),
                givenName = HttpContext.Session.GetString(BffSessionKeys.UserGivenName),
                familyName = HttpContext.Session.GetString(BffSessionKeys.UserFamilyName)
            },
            csrfToken = csrf,
            expiresAtUnix = BffSessionStore.GetExpiresAtUnix(HttpContext.Session)
        });
    }

    /// <summary>
    /// Clears local session and invalidates Keycloak refresh token (if present).
    /// Requires CSRF (header + cookie).
    /// </summary>
    [HttpGet("logout")]
    public async Task<IActionResult> LogoutRedirect([FromQuery] string? returnUrl = null)
    {
        var redirectUri = BuildFrontendLogoutRedirectUrl(returnUrl);
        await PerformLocalLogoutAsync();

        var keycloakLogoutUrl = BuildKeycloakLogoutUrl(redirectUri);
        return Redirect(keycloakLogoutUrl);
    }

    [HttpPost("logout")]
    public async Task<IActionResult> Logout()
    {
        await PerformLocalLogoutAsync();
        return NoContent();
    }

    /// <summary>
    /// Inicia o fluxo de recuperação de senha (anti-enumeração: sempre responde OK).
    /// </summary>
    [HttpPost("forgot-password")]
    public async Task<IActionResult> ForgotPassword(
        [FromBody] ForgotPasswordRequest request,
        CancellationToken cancellationToken)
    {
        var defaultResponse = Ok(new { message = "Se o email existir, enviaremos instruções para redefinir a senha." });

        if (request == null || string.IsNullOrWhiteSpace(request.Email))
            return defaultResponse;

        var command = new GeneratePasswordResetTokenCommand
        {
            Email = request.Email
        };

        var result = await _mediator.Send(command, cancellationToken);

        if (!result.Success)
            return defaultResponse;

        var resetUrl = BuildResetPasswordUrl(result.Email, result.Token!);

        await _emailService.SendEmailWithTemplateAsync(
            templateType: AssistenteExecutivo.Domain.Notifications.EmailTemplateType.PasswordReset,
            recipientEmail: result.Email,
            recipientName: result.FullName,
            templateValues: new Dictionary<string, object>
            {
                { "NomeUsuario", result.FullName },
                { "ResetSenhaUrl", resetUrl }
            },
            cancellationToken: cancellationToken);

        return defaultResponse;
    }

    /// <summary>
    /// Finaliza a recuperação de senha usando token gerado no domínio (UserProfile).
    /// </summary>
    [HttpPost("reset-password")]
    public async Task<IActionResult> ResetPassword(
        [FromBody] ResetPasswordRequest request,
        CancellationToken cancellationToken)
    {
        if (request == null
            || string.IsNullOrWhiteSpace(request.Email)
            || string.IsNullOrWhiteSpace(request.Token)
            || string.IsNullOrWhiteSpace(request.NewPassword))
        {
            return BadRequest(new { message = "Requisição inválida." });
        }

        var command = new ResetPasswordCommand
        {
            Email = request.Email,
            Token = request.Token,
            NewPassword = request.NewPassword
        };

        var result = await _mediator.Send(command, cancellationToken);

        if (!result.Success)
        {
            return BadRequest(new { message = result.ErrorMessage ?? "Erro ao resetar senha." });
        }

        return Ok(new { message = "Senha atualizada com sucesso." });
    }

    private string BuildResetPasswordUrl(string email, string token)
    {
        var frontendBaseUrl = _configuration["Frontend:BaseUrl"] 
            ?? throw new InvalidOperationException("Frontend:BaseUrl não configurado em appsettings");
        var basePath = $"{frontendBaseUrl.TrimEnd('/')}/reset-senha";
        var qs = $"email={Uri.EscapeDataString(email)}&token={Uri.EscapeDataString(token)}";
        return $"{basePath}?{qs}";
    }

    private string GetRealm()
        => _configuration["Keycloak:Realm"] ?? "assistenteexecutivo";

    private string GetKeycloakBaseUrl()
        => (_configuration["Keycloak:BaseUrl"] 
            ?? throw new InvalidOperationException("Keycloak:BaseUrl não configurado em appsettings")).TrimEnd('/');

    private string GetClientId()
        => _configuration["Keycloak:ClientId"] ?? "assistenteexecutivo-app";

    private string GetCallbackRedirectUri()
    {
        // Sempre calcular pelo request (depois de UseForwardedHeaders),
        // para funcionar tanto em localhost quanto via hostname externo.
        var redirectBase = $"{Request.Scheme}://{Request.Host}".TrimEnd('/');
        return $"{redirectBase}/auth/oauth-callback";
    }

    private string BuildDefaultAuthorizeUrl(string realm, string redirectUri, string state)
    {
        var keycloakUrl = GetEffectiveKeycloakBaseUrl();
        var authUrl = $"{keycloakUrl}/realms/{realm}/protocol/openid-connect/auth";
        var parameters = new Dictionary<string, string>
        {
            { "client_id", GetClientId() },
            { "redirect_uri", redirectUri },
            { "response_type", "code" },
            { "scope", "openid profile email" },
            { "state", state }
        };

        var themeName = _configuration["Keycloak:ThemeName"];
        if (!string.IsNullOrWhiteSpace(themeName))
        {
            parameters["kc_theme"] = themeName;
        }

        var queryString = string.Join("&",
            parameters.Select(p => $"{Uri.EscapeDataString(p.Key)}={Uri.EscapeDataString(p.Value)}"));

        return $"{authUrl}?{queryString}";
    }

    private static string NormalizeReturnPath(string? returnUrl)
    {
        if (string.IsNullOrWhiteSpace(returnUrl))
            return "/";

        // Only allow relative paths to prevent open redirects.
        if (Uri.TryCreate(returnUrl, UriKind.Relative, out _) && returnUrl.StartsWith("/", StringComparison.Ordinal))
            return returnUrl;

        return "/";
    }

    private string BuildFrontendRedirectUrl(string? appendQuery = null)
    {
        var baseUrl = (HttpContext.Session.GetString(BffSessionKeys.FrontendBaseUrl)
            ?? _configuration["Frontend:BaseUrl"]
            ?? throw new InvalidOperationException("Frontend:BaseUrl não configurado em appsettings")).TrimEnd('/');
        var returnPath = HttpContext.Session.GetString(BffSessionKeys.ReturnPath) ?? "/";

        var url = $"{baseUrl}{NormalizeReturnPath(returnPath)}";
        if (string.IsNullOrWhiteSpace(appendQuery))
            return url;

        var separator = url.Contains('?', StringComparison.Ordinal) ? "&" : "?";
        return $"{url}{separator}{appendQuery}";
    }

    private void SetCsrfCookie(string token)
    {
        // NOTE: `SameSite=None` cookies must also be `Secure`. On HTTP localhost, the browser will drop the cookie.
        var apiBaseUrl = _configuration["Api:BaseUrl"];
        var useSecureCookies =
            Request.IsHttps ||
            (!string.IsNullOrWhiteSpace(apiBaseUrl) && apiBaseUrl.StartsWith("https://", StringComparison.OrdinalIgnoreCase));

        var cookieOptions = new CookieOptions
        {
            HttpOnly = false,
            IsEssential = true,
            // Usar SameSite=None com Secure para funcionar em redirects cross-subdomain
            SameSite = useSecureCookies ? SameSiteMode.None : SameSiteMode.Lax,
            // Sempre usar Secure quando SameSite=None (requisito do navegador)
            Secure = useSecureCookies
        };
        
        // Configurar Domain para funcionar entre subdomínios
        var domain = GetCookieDomain();
        if (!string.IsNullOrWhiteSpace(domain))
        {
            cookieOptions.Domain = domain;
        }
        
        Response.Cookies.Append(BffCsrf.CookieName, token, cookieOptions);
    }

    private void DeleteCookie(string name)
    {
        var domain = GetCookieDomain();
        if (string.IsNullOrWhiteSpace(domain))
        {
            Response.Cookies.Delete(name);
            return;
        }

        Response.Cookies.Delete(name, new CookieOptions
        {
            Domain = domain,
            Path = "/"
        });
    }

    private void DeleteBffCookies()
    {
        DeleteCookie("ae.sid");
        DeleteCookie(BffCsrf.CookieName);
    }

    private async Task PerformLocalLogoutAsync()
    {
        var realm = GetRealm();
        var refreshToken = HttpContext.Session.GetString(BffSessionKeys.RefreshToken);

        if (!string.IsNullOrWhiteSpace(refreshToken))
        {
            try
            {
                await _keycloakService.LogoutAsync(realm, refreshToken, HttpContext.RequestAborted);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Falha ao invalidar refresh token no Keycloak durante logout.");
            }
        }

        HttpContext.Session.Clear();
        try
        {
            await HttpContext.Session.CommitAsync(HttpContext.RequestAborted);
        }
        catch
        {
        }

        DeleteBffCookies();
    }

    private string BuildFrontendLogoutRedirectUrl(string? returnUrl)
    {
        var fromHeaders = TryGetBaseUrlFromHeaders("Origin") ?? TryGetBaseUrlFromHeaders("Referer");
        var fallback = _configuration["Frontend:BaseUrl"]
            ?? throw new InvalidOperationException("Frontend:BaseUrl não configurado em appsettings");

        var baseUrl = (fromHeaders ?? fallback).TrimEnd('/');
        var path = string.IsNullOrWhiteSpace(returnUrl) ? "/login" : returnUrl;
        return $"{baseUrl}{NormalizeReturnPath(path)}";
    }

    private string BuildKeycloakLogoutUrl(string postLogoutRedirectUri)
    {
        var realm = GetRealm();
        var keycloakBaseUrl = GetEffectiveKeycloakBaseUrl().TrimEnd('/');
        var clientId = GetClientId();

        var redirect = Uri.EscapeDataString(postLogoutRedirectUri);
        var cid = Uri.EscapeDataString(clientId);

        // Enviar tanto `post_logout_redirect_uri` quanto `redirect_uri` para maximizar compatibilidade entre versAµes do Keycloak.
        return $"{keycloakBaseUrl}/realms/{realm}/protocol/openid-connect/logout?client_id={cid}&post_logout_redirect_uri={redirect}&redirect_uri={redirect}";
    }
    
    private string? GetCookieDomain()
    {
        var apiBaseUrl = _configuration["Api:BaseUrl"];
        if (string.IsNullOrWhiteSpace(apiBaseUrl) || !Uri.TryCreate(apiBaseUrl, UriKind.Absolute, out var apiUri))
            return null;
        
        var host = apiUri.Host;
        var parts = host.Split('.');
        if (parts.Length < 2)
            return null;
        
        // Pegar os últimos 2 ou 3 segmentos (ex: callback-local-cchagas.xyz ou exemplo.com.br)
        var domainBase = parts.Length >= 3 && parts[parts.Length - 2].Length <= 3 
            ? string.Join(".", parts.Skip(parts.Length - 3)) // Para .com.br, .co.uk, etc
            : string.Join(".", parts.Skip(parts.Length - 2)); // Para .com, .xyz, etc
        
        // Retornar com ponto inicial para funcionar em todos os subdomínios
        return $".{domainBase}";
    }

    private static string GenerateState()
    {
        return Base64UrlEncode(RandomNumberGenerator.GetBytes(32));
    }

    private static string Base64UrlEncode(byte[] bytes)
    {
        return Convert.ToBase64String(bytes)
            .TrimEnd('=')
            .Replace('+', '-')
            .Replace('/', '_');
    }

    private static bool FixedTimeEquals(string a, string b)
    {
        var aBytes = System.Text.Encoding.UTF8.GetBytes(a);
        var bBytes = System.Text.Encoding.UTF8.GetBytes(b);
        return CryptographicOperations.FixedTimeEquals(aBytes, bBytes);
    }

    private void StoreFrontendBaseUrlForRedirect()
    {
        var fromHeaders = TryGetBaseUrlFromHeaders("Origin") ?? TryGetBaseUrlFromHeaders("Referer");
        var fallback = _configuration["Frontend:BaseUrl"]
            ?? throw new InvalidOperationException("Frontend:BaseUrl não configurado em appsettings");

        var baseUrl = (fromHeaders ?? fallback).TrimEnd('/');
        HttpContext.Session.SetString(BffSessionKeys.FrontendBaseUrl, baseUrl);
    }

    private string? TryGetBaseUrlFromHeaders(string headerName)
    {
        var raw = Request.Headers[headerName].ToString();
        if (string.IsNullOrWhiteSpace(raw))
            return null;

        if (!Uri.TryCreate(raw, UriKind.Absolute, out var uri))
            return null;

        return $"{uri.Scheme}://{uri.Authority}";
    }

    private string BuildFrontendLoginUrlWithError(string authError)
    {
        var baseUrl = (HttpContext.Session.GetString(BffSessionKeys.FrontendBaseUrl)
            ?? _configuration["Frontend:BaseUrl"]
            ?? throw new InvalidOperationException("Frontend:BaseUrl não configurado em appsettings")).TrimEnd('/');

        var returnPath = HttpContext.Session.GetString(BffSessionKeys.ReturnPath) ?? "/dashboard";
        var qs = $"authError={Uri.EscapeDataString(authError)}&returnUrl={Uri.EscapeDataString(NormalizeReturnPath(returnPath))}";
        return $"{baseUrl}/login?{qs}";
    }

    private string GetEffectiveKeycloakBaseUrl()
    {
        return GetKeycloakBaseUrl();
    }

    private bool IsLocalhostRequest()
    {
        var host = Request.Host.Host;
        return string.Equals(host, "localhost", StringComparison.OrdinalIgnoreCase)
            || string.Equals(host, "127.0.0.1", StringComparison.OrdinalIgnoreCase)
            || string.Equals(host, "::1", StringComparison.OrdinalIgnoreCase);
    }
}
