# Fix: Cookies Cross-Subdomain - invalid_state em Produção

## Problema

Em produção com `api.assistente.live` e `web.assistente.live`, estava ocorrendo:
- Erro `invalid_state` no callback OAuth
- Cookie `ae.sid` não sendo criado
- Cookie `NEXT_LOCALE` não sendo criado

## Causa

Os cookies não estavam configurados com `Domain` cross-subdomain, então:
1. Quando o usuário fazia login em `web.assistente.live` → redirecionava para `api.assistente.live`
2. A API criava o cookie `ae.sid` sem `Domain`, então o cookie só funcionava em `api.assistente.live`
3. Quando o Keycloak redirecionava de volta para `api.assistente.live/auth/oauth-callback`, a sessão não era encontrada porque o cookie não estava sendo enviado corretamente

## Correções Aplicadas

### 1. Backend (Program.cs) - Cookie de Sessão `ae.sid`

**Arquivo**: `backend/src/AssistenteExecutivo.Api/Program.cs`

**Mudança**: Agora usa `Api:PublicBaseUrl` (se disponível) em vez de `Api:BaseUrl` para calcular o domain do cookie.

```csharp
// Antes: usava Api:BaseUrl
var apiBaseUrl = builder.Configuration["Api:BaseUrl"];

// Depois: usa Api:PublicBaseUrl se disponível
var apiPublicBaseUrl = builder.Configuration["Api:PublicBaseUrl"] ?? apiBaseUrl;
```

**Resultado**: Para `api.assistente.live`, o cookie será configurado com `Domain=.assistente.live`, funcionando em todos os subdomínios.

### 2. Frontend (middleware.ts) - Cookie `NEXT_LOCALE`

**Arquivo**: `web/middleware.ts`

**Mudança**: Agora configura o cookie `NEXT_LOCALE` com `Domain` cross-subdomain quando necessário.

```typescript
// Antes: cookie sem domain
res.cookies.set("NEXT_LOCALE", locale, { path: "/", sameSite: "lax" });

// Depois: cookie com domain cross-subdomain
const cookieDomain = getCookieDomain(req); // Calcula .assistente.live
res.cookies.set("NEXT_LOCALE", locale, {
  path: "/",
  sameSite: cookieDomain && isHttps ? "none" : "lax",
  ...(cookieDomain && isHttps && { domain: cookieDomain, secure: true })
});
```

**Resultado**: O cookie `NEXT_LOCALE` agora funciona em todos os subdomínios.

### 3. Logs de Debug Adicionados

**Arquivo**: `backend/src/AssistenteExecutivo.Api/Controllers/AuthController.cs`

**Mudança**: Adicionados logs detalhados no método `OAuthCallback` para diagnosticar problemas de sessão.

```csharp
_logger.LogInformation(
    "OAuthCallback - SessionId: {SessionId}, HasSessionCookie: {HasSessionCookie}, ...",
    sessionId, hasSessionCookie, ...);
```

## Configuração Necessária em Produção

### Variáveis de Ambiente - API

Certifique-se de que as seguintes variáveis estão configuradas no Cloud Run:

```bash
# OBRIGATÓRIA: URL pública da API (usada para calcular domain do cookie)
Api__PublicBaseUrl="https://api.assistente.live"

# OBRIGATÓRIA: URL pública do frontend
Frontend__BaseUrl="https://web.assistente.live"

# OBRIGATÓRIA: URL pública do Keycloak
Keycloak__PublicBaseUrl="https://auth.assistente.live"  # ou sua URL do Keycloak
```

### Variáveis de Ambiente - Web

```bash
# OBRIGATÓRIA: URL da API (usada no frontend)
NEXT_PUBLIC_API_BASE_URL="https://api.assistente.live"
```

**⚠️ IMPORTANTE**: `NEXT_PUBLIC_API_BASE_URL` precisa ser configurada durante o **build** (não em runtime). Veja `web/VARIAVEIS_AMBIENTE_NEXTJS.md` para mais detalhes.

## Verificação

### 1. Verificar Cookies no Navegador

1. Abra o DevTools (F12)
2. Vá para **Application** → **Cookies**
3. Verifique se os cookies estão sendo criados:
   - `ae.sid` (Domain: `.assistente.live`)
   - `NEXT_LOCALE` (Domain: `.assistente.live`)
   - `XSRF-TOKEN` (Domain: `.assistente.live`)

### 2. Verificar Atributos dos Cookies

Os cookies devem ter:
- **Domain**: `.assistente.live` (com ponto inicial)
- **SameSite**: `None` (para cookies cross-subdomain)
- **Secure**: ✅ (obrigatório com SameSite=None)
- **HttpOnly**: ✅ (apenas para `ae.sid`)

### 3. Verificar Logs

Após fazer login, verifique os logs do Cloud Run:

```bash
gcloud run services logs read assistente-api --region us-central1 --limit 50
```

Procure por:
- `OAuthCallback - SessionId: ...` - Deve mostrar `HasSessionCookie: true`
- `Cookie domain configurado: .assistente.live` - Confirma que o domain foi calculado corretamente

### 4. Testar Fluxo Completo

1. Acesse `https://web.assistente.live/login`
2. Clique em "Login"
3. Faça login no Keycloak
4. Verifique se é redirecionado para o dashboard sem erro `invalid_state`

## Troubleshooting

### Se ainda aparecer `invalid_state`

1. **Verificar se `Api__PublicBaseUrl` está configurada**:
   ```bash
   gcloud run services describe assistente-api \
     --region us-central1 \
     --format="value(spec.template.spec.containers[0].env)"
   ```

2. **Verificar se o cookie está sendo enviado**:
   - Abra DevTools → Network
   - Veja a requisição para `/auth/oauth-callback`
   - Verifique se o header `Cookie` contém `ae.sid=...`

3. **Verificar se o cookie está sendo criado**:
   - Veja a requisição para `/auth/login`
   - Verifique se o header `Set-Cookie` contém `ae.sid=...; Domain=.assistente.live`

4. **Limpar cookies e tentar novamente**:
   - No DevTools → Application → Cookies
   - Delete todos os cookies de `.assistente.live`
   - Tente fazer login novamente

### Se o cookie não estiver sendo criado

1. **Verificar se está usando HTTPS**: Cookies com `SameSite=None` requerem HTTPS
2. **Verificar se o domain está correto**: Deve ser `.assistente.live` (com ponto inicial)
3. **Verificar logs**: Procure por erros relacionados a cookies nos logs

## Próximos Passos

Após aplicar essas correções:

1. ✅ Fazer rebuild do frontend (se necessário)
2. ✅ Fazer deploy da API com as novas variáveis
3. ✅ Testar o fluxo de login completo
4. ✅ Verificar cookies no navegador
5. ✅ Verificar logs para confirmar que está funcionando

## Referências

- [KEYCLOAK_SESSION_STATE_FIX.md](./KEYCLOAK_SESSION_STATE_FIX.md) - Documentação sobre problemas de sessão
- [ENV_VARIABLES.md](./ENV_VARIABLES.md) - Lista completa de variáveis de ambiente
- [web/VARIAVEIS_AMBIENTE_NEXTJS.md](./web/VARIAVEIS_AMBIENTE_NEXTJS.md) - Variáveis do Next.js

