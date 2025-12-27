# Entendendo os Redirect URIs do Keycloak

## 🔄 Dois Tipos de Redirect URIs

Existem **dois redirect URIs diferentes** que servem propósitos diferentes no fluxo OAuth:

### 1. GoogleRedirectUri (Google → Keycloak)
```
https://auth.callback-local-cchagas.xyz/realms/assistenteexecutivo/broker/google/endpoint
```

**Propósito:** Este é o redirect URI que o **Google OAuth** usa para redirecionar de volta para o **Keycloak** após o usuário autenticar com Google.

**Fluxo:**
1. Usuário clica em "Login com Google"
2. É redirecionado para Google
3. Google autentica o usuário
4. Google redireciona para: `/broker/google/endpoint` (dentro do Keycloak)
5. Keycloak processa e cria/vincula o usuário

**Onde é configurado:**
- No **Identity Provider do Google** dentro do Keycloak
- Configurado via `Keycloak:GoogleRedirectUri` no appsettings
- Usado pelo método `ConfigureGoogleIdentityProviderAsync()`

### 2. OAuth Callback URI (Keycloak → Aplicação)
```
https://assistente-api-174965982696.us-central1.run.app/auth/oauth-callback
```

**Propósito:** Este é o redirect URI que o **Keycloak** usa para redirecionar de volta para a **sua aplicação** após completar a autenticação (seja com Google, username/password, etc).

**Fluxo:**
1. Usuário inicia login na aplicação
2. Aplicação redireciona para Keycloak
3. Keycloak autentica o usuário (pode usar Google, username/password, etc)
4. Keycloak redireciona para: `/auth/oauth-callback` (na sua aplicação)
5. Aplicação recebe o código de autorização e troca por tokens

**Onde é configurado:**
- No **Cliente do Keycloak** (`assistenteexecutivo-app`)
- Configurado via `Api:PublicBaseUrl` no appsettings
- Usado pelo método `BuildRedirectUris()` no `KeycloakService`
- Registrado automaticamente quando a aplicação inicia

## 📊 Fluxo Completo de Autenticação

```
┌─────────┐         ┌──────────┐         ┌─────────┐         ┌──────────┐
│ Usuário │         │ Aplicação│         │Keycloak │         │  Google  │
└────┬────┘         └────┬─────┘         └────┬────┘         └────┬─────┘
     │                    │                     │                    │
     │ 1. Clica "Login"   │                     │                    │
     ├───────────────────>│                     │                    │
     │                    │                     │                    │
     │                    │ 2. GET /auth/login   │                    │
     │                    │    provider=google   │                    │
     │                    ├─────────────────────>│                    │
     │                    │                     │                    │
     │                    │                     │ 3. Redirect Google  │
     │                    │                     ├─────────────────────>│
     │                    │                     │                    │
     │                    │                     │ 4. Usuário autentica│
     │                    │                     │<────────────────────┤
     │                    │                     │                    │
     │                    │                     │ 5. Redirect para    │
     │                    │                     │    /broker/google/  │
     │                    │                     │    endpoint         │
     │                    │                     │<────────────────────┤
     │                    │                     │                    │
     │                    │                     │ 6. Processa e cria │
     │                    │                     │    sessão Keycloak  │
     │                    │                     │                    │
     │                    │ 7. Redirect para    │                    │
     │                    │    /auth/oauth-     │                    │
     │                    │    callback?code=.. │                    │
     │                    │<────────────────────┤                    │
     │                    │                     │                    │
     │                    │ 8. Troca code por   │                    │
     │                    │    tokens           │                    │
     │                    ├─────────────────────>│                    │
     │                    │                     │                    │
     │                    │ 9. Cria sessão BFF  │                    │
     │                    │    e redireciona     │                    │
     │                    │    para frontend     │                    │
     │<───────────────────┤                     │                    │
     │                    │                     │                    │
```

## 🔧 Configuração

### GoogleRedirectUri (Google → Keycloak)

Configurado no `appsettings.json`:

```json
{
  "Keycloak": {
    "GoogleRedirectUri": "https://auth.callback-local-cchagas.xyz/realms/assistenteexecutivo/broker/google/endpoint"
  }
}
```

**Importante:** Este URI deve ser:
- Acessível publicamente (HTTPS)
- Apontar para o Keycloak (não para sua aplicação)
- Configurado no Google Cloud Console como redirect URI válido

### OAuth Callback URI (Keycloak → Aplicação)

Configurado via variável de ambiente:

```bash
Api__PublicBaseUrl="https://assistente-api-174965982696.us-central1.run.app"
```

O `KeycloakService` automaticamente constrói o redirect URI como:
```
{Api__PublicBaseUrl}/auth/oauth-callback
```

**Importante:** Este URI deve ser:
- Acessível publicamente (HTTPS)
- Apontar para sua aplicação (não para o Keycloak)
- Registrado no cliente do Keycloak como redirect URI válido

## ❓ Por que o Erro Aconteceu?

O erro `Invalid parameter: redirect_uri` aconteceu porque:

1. A aplicação está rodando no Cloud Run com URL: `https://assistente-api-174965982696.us-central1.run.app`
2. O `AuthController` constrói o redirect URI dinamicamente usando `Request.Host`
3. O redirect URI gerado foi: `https://assistente-api-174965982696.us-central1.run.app/auth/oauth-callback`
4. Mas esse URI **não estava registrado** no cliente do Keycloak
5. O Keycloak rejeitou a requisição por segurança

## ✅ Solução

Configure `Api__PublicBaseUrl` para que o `KeycloakService` registre automaticamente o redirect URI:

```bash
gcloud run services update assistente-api \
  --region us-central1 \
  --update-env-vars Api__PublicBaseUrl="https://assistente-api-174965982696.us-central1.run.app"
```

Após reiniciar, o `KeycloakService` vai:
1. Detectar que `Api__PublicBaseUrl` está configurado
2. Construir o redirect URI: `https://assistente-api-174965982696.us-central1.run.app/auth/oauth-callback`
3. Registrar esse URI no cliente do Keycloak automaticamente

## 📝 Resumo

| Tipo | URI | De → Para | Configuração |
|------|-----|-----------|--------------|
| **GoogleRedirectUri** | `/broker/google/endpoint` | Google → Keycloak | `Keycloak:GoogleRedirectUri` |
| **OAuth Callback** | `/auth/oauth-callback` | Keycloak → Aplicação | `Api__PublicBaseUrl` |

Ambos são necessários e servem propósitos diferentes no fluxo OAuth!



