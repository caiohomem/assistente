# Fix: Invalid State - Problema de Sessão

## Problema

Após fazer login, você recebe o erro:
```json
{"error":"invalid_state","message":"State inválido."}
```

## Causa

O erro ocorre porque a **sessão não está sendo mantida** entre a requisição de login e o callback do OAuth. O `state` é armazenado na sessão durante o login, mas quando o Keycloak redireciona de volta para `/auth/oauth-callback`, a sessão não é encontrada.

### Por que a sessão se perde?

No Cloud Run, as sessões podem se perder por:

1. **Cookies não estão sendo enviados** - Problema de `SameSite` ou `Domain`
2. **Sessão em memória** - PostgreSQL usa `MemoryCache` que não persiste entre requisições
3. **Cookie não está sendo definido corretamente** - Configuração de `Domain` ou `Secure`

## Solução

### 1. Verificar Configuração de Cookies

A configuração de sessão no `Program.cs` já está configurada para funcionar com HTTPS e cross-domain:

```csharp
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(30);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
    options.Cookie.SameSite = SameSiteMode.None;  // Permite cross-domain
    options.Cookie.SecurePolicy = CookieSecurePolicy.Always;  // Requer HTTPS
    options.Cookie.Name = "ae.sid";
    
    // Configurar Domain para funcionar entre subdomínios
    if (!string.IsNullOrWhiteSpace(apiPublicBaseUrl) && Uri.TryCreate(apiPublicBaseUrl, UriKind.Absolute, out var apiUri))
    {
        var host = apiUri.Host;
        var parts = host.Split('.');
        if (parts.Length >= 2)
        {
            var domainBase = parts.Length >= 3 && parts[parts.Length - 2].Length <= 3 
                ? string.Join(".", parts.Skip(parts.Length - 3))
                : string.Join(".", parts.Skip(parts.Length - 2));
            
            options.Cookie.Domain = $".{domainBase}";  // Ex: .run.app
        }
    }
});
```

### 2. Verificar Variáveis de Ambiente

Certifique-se de que `Api__PublicBaseUrl` está configurado corretamente:

```bash
gcloud run services describe assistente-api \
  --region us-central1 \
  --format="value(spec.template.spec.containers[0].env)"
```

Deve conter:
```
Api__PublicBaseUrl=https://assistente-api-174965982696.us-central1.run.app
```

### 3. Verificar Cookies no Navegador

1. Abra o DevTools (F12)
2. Vá para **Application** → **Cookies**
3. Verifique se o cookie `ae.sid` está sendo criado
4. Verifique os atributos do cookie:
   - **Domain**: Deve ser `.run.app` ou similar
   - **SameSite**: Deve ser `None`
   - **Secure**: Deve estar marcado

### 4. Problema com PostgreSQL + MemoryCache

Se você está usando PostgreSQL, a sessão está sendo armazenada em `MemoryCache` (não persiste). Isso significa que:

- Se a aplicação reiniciar, as sessões são perdidas
- Se houver múltiplas instâncias, cada uma tem seu próprio cache

**Solução Temporária:** Use Redis ou SQL Server para sessões distribuídas.

**Solução Imediata:** Verifique se o cookie está sendo enviado corretamente.

### 5. Debug: Verificar Logs

Adicione logs temporários para verificar se a sessão está sendo mantida:

```csharp
// No AuthController, método Login
var state = GenerateState();
HttpContext.Session.SetString(BffSessionKeys.OAuthState, state);
await HttpContext.Session.CommitAsync();  // IMPORTANTE: Commit explícito
_logger.LogInformation("State armazenado na sessão: {State}, SessionId: {SessionId}", 
    state, HttpContext.Session.Id);

// No AuthController, método OAuthCallback
var expectedState = HttpContext.Session.GetString(BffSessionKeys.OAuthState);
_logger.LogInformation("State recuperado da sessão: {ExpectedState}, SessionId: {SessionId}, Received: {ReceivedState}",
    expectedState, HttpContext.Session.Id, state);
```

## Soluções Específicas

### Solução 1: Commit Explícito da Sessão

No método `Login`, adicione um commit explícito:

```csharp
[HttpGet("login")]
public async Task<IActionResult> Login([FromQuery] string? provider = null, [FromQuery] string? returnUrl = null)
{
    // ... código existente ...
    
    var state = GenerateState();
    HttpContext.Session.SetString(BffSessionKeys.OAuthState, state);
    HttpContext.Session.SetString(BffSessionKeys.ReturnPath, NormalizeReturnPath(returnUrl));
    
    // IMPORTANTE: Commit explícito antes do redirect
    await HttpContext.Session.CommitAsync(HttpContext.RequestAborted);
    
    // ... resto do código ...
}
```

### Solução 2: Verificar Domain do Cookie

Se o cookie não está sendo enviado, pode ser problema de domain. Verifique se o domain está correto:

- Se sua API está em: `assistente-api-174965982696.us-central1.run.app`
- O domain do cookie deve ser: `.run.app` (com ponto inicial)

### Solução 3: Usar Redis para Sessões (Recomendado)

Para produção, use Redis para sessões distribuídas:

```bash
# Instalar pacote (já deve estar instalado)
# Microsoft.Extensions.Caching.StackExchangeRedis

# Configurar no Program.cs
builder.Services.AddStackExchangeRedisCache(options =>
{
    options.Configuration = "your-redis-connection-string";
    options.InstanceName = "AssistenteExecutivo:";
});
```

E configure a variável de ambiente:
```bash
Redis__ConnectionString="your-redis-connection-string"
```

## Verificação Rápida

1. **Faça login novamente**
2. **Antes de clicar em "Login com Google"**, abra o DevTools → Network
3. **Verifique a requisição** para `/auth/login`
4. **Veja os headers de resposta** - deve ter `Set-Cookie: ae.sid=...`
5. **Após o redirect do Keycloak**, verifique se o cookie `ae.sid` está sendo enviado na requisição para `/auth/oauth-callback`

## Logs para Verificar

Procure nos logs do Cloud Run:

```bash
gcloud run services logs read assistente-api --region us-central1 --limit 100
```

Procure por:
- `State armazenado na sessão`
- `State recuperado da sessão`
- `State inválido ou não encontrado`

## Solução Imediata

Se nada funcionar, você pode temporariamente desabilitar a validação de state (NÃO RECOMENDADO para produção):

```csharp
// TEMPORÁRIO - APENAS PARA DEBUG
var expectedState = HttpContext.Session.GetString(BffSessionKeys.OAuthState);
if (string.IsNullOrWhiteSpace(expectedState) || !FixedTimeEquals(expectedState, state))
{
    _logger.LogWarning("State inválido - IGNORANDO TEMPORARIAMENTE para debug");
    // return BadRequest(new { error = "invalid_state", message = "State inválido." });
    // Comentar a linha acima temporariamente
}
```

**⚠️ ATENÇÃO:** Isso remove a proteção CSRF. Use apenas para debug e remova antes de ir para produção!

