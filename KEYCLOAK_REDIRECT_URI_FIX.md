# Fix: Invalid parameter: redirect_uri

## Problema

O erro `Invalid parameter: redirect_uri` ocorre porque o redirect URI usado pela aplicação não está configurado como válido no cliente do Keycloak.

**Redirect URI sendo usado:**
```
https://assistente-api-174965982696.us-central1.run.app/auth/oauth-callback
```

## Solução

### Opção 1: Configurar Variável de Ambiente (Recomendado)

Configure a variável `Api__PublicBaseUrl` no Cloud Run para que o KeycloakService registre automaticamente o redirect URI correto:

```bash
gcloud run services update assistente-api \
  --region us-central1 \
  --update-env-vars Api__PublicBaseUrl="https://assistente-api-174965982696.us-central1.run.app"
```

**Importante:** Após configurar, a aplicação precisa ser reiniciada para que o `KeycloakService` registre o novo redirect URI no Keycloak.

### Opção 2: Adicionar Manualmente no Keycloak

1. Acesse o Keycloak Admin Console
2. Vá para **Clients** → `assistenteexecutivo-app`
3. Na aba **Settings**, encontre **Valid Redirect URIs**
4. Adicione:
   ```
   https://assistente-api-174965982696.us-central1.run.app/auth/oauth-callback
   ```
5. Clique em **Save**

### Opção 3: Usar Wildcard (Menos Seguro)

Se você quiser permitir qualquer subdomínio do Cloud Run:

1. No Keycloak Admin Console
2. **Clients** → `assistenteexecutivo-app` → **Settings**
3. Em **Valid Redirect URIs**, adicione:
   ```
   https://assistente-api-*.us-central1.run.app/auth/oauth-callback
   ```
4. Clique em **Save**

**⚠️ Atenção:** Wildcards podem ser menos seguros. Use apenas se necessário.

## Como o Redirect URI é Construído

### No AuthController

O método `GetCallbackRedirectUri()` usa o request atual:

```csharp
private string GetCallbackRedirectUri()
{
    var redirectBase = $"{Request.Scheme}://{Request.Host}".TrimEnd('/');
    return $"{redirectBase}/auth/oauth-callback";
}
```

Isso significa que o redirect URI será sempre baseado na URL atual da requisição.

### No KeycloakService

O método `BuildRedirectUris()` constrói a lista de URIs válidos baseado nas configurações:

```csharp
private string[] BuildRedirectUris()
{
    // Usa Api:PublicBaseUrl se configurado
    var apiPublicBaseUrl = _configuration["Api:PublicBaseUrl"];
    if (!string.IsNullOrWhiteSpace(apiPublicBaseUrl))
    {
        set.Add($"{apiPublicBaseUrl}/auth/oauth-callback");
    }
    
    // Fallback para Api:BaseUrl
    var apiBaseUrl = _configuration["Api:BaseUrl"];
    if (!string.IsNullOrWhiteSpace(apiBaseUrl))
    {
        set.Add($"{apiBaseUrl}/auth/oauth-callback");
    }
}
```

## Variáveis de Ambiente Necessárias

Para que o KeycloakService registre automaticamente os redirect URIs, configure:

```bash
# URL pública da API (usada em produção)
Api__PublicBaseUrl="https://assistente-api-174965982696.us-central1.run.app"

# URL interna da API (usada em desenvolvimento)
Api__BaseUrl="http://localhost:5239"
```

## Verificação

Após configurar, verifique se o redirect URI foi registrado:

1. Acesse o Keycloak Admin Console
2. **Clients** → `assistenteexecutivo-app` → **Settings**
3. Verifique se o redirect URI aparece em **Valid Redirect URIs**

Ou via API do Keycloak:

```bash
# Obter token admin
TOKEN=$(curl -X POST "https://auth.callback-local-cchagas.xyz/realms/master/protocol/openid-connect/token" \
  -H "Content-Type: application/x-www-form-urlencoded" \
  -d "username=admin" \
  -d "password=admin" \
  -d "grant_type=password" \
  -d "client_id=admin-cli" | jq -r '.access_token')

# Listar redirect URIs do cliente
curl -X GET "https://auth.callback-local-cchagas.xyz/admin/realms/assistenteexecutivo/clients?clientId=assistenteexecutivo-app" \
  -H "Authorization: Bearer $TOKEN" | jq '.[0].redirectUris'
```

## Troubleshooting

### O redirect URI ainda não funciona

1. **Verifique se a variável está configurada:**
   ```bash
   gcloud run services describe assistente-api --region us-central1 --format="value(spec.template.spec.containers[0].env)"
   ```

2. **Verifique os logs da aplicação:**
   ```bash
   gcloud run services logs read assistente-api --region us-central1 --limit 50
   ```
   Procure por: `Config client redirectUris` ou `Atualizando client redirectUris`

3. **Force a atualização do cliente:**
   - A aplicação atualiza o cliente automaticamente na inicialização
   - Reinicie o serviço Cloud Run para forçar a atualização

### Múltiplos Ambientes

Se você tem múltiplos ambientes (dev, staging, prod), configure redirect URIs para cada um:

```
https://assistente-api-dev-xxx.run.app/auth/oauth-callback
https://assistente-api-staging-xxx.run.app/auth/oauth-callback
https://assistente-api-xxx.run.app/auth/oauth-callback
```

Ou use wildcards se apropriado (menos seguro).

## Referências

- [Keycloak Client Settings](https://www.keycloak.org/docs/latest/server_admin/#_clients)
- [OAuth 2.0 Redirect URI](https://tools.ietf.org/html/rfc6749#section-3.1.2)

