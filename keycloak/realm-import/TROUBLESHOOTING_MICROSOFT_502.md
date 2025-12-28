# Troubleshooting - Erro 502 no Callback do Microsoft Identity Provider

## Problema
Erro 502 ao acessar o callback do Microsoft:
```
https://keycloak.callback-local-cchagas.xyz/realms/assistenteexecutivo/broker/microsoft/endpoint
```

## Possíveis Causas e Soluções

### 1. Verificar Configuração no Azure Portal

**Redirect URI deve ser exatamente:**
```
https://keycloak.callback-local-cchagas.xyz/realms/assistenteexecutivo/broker/microsoft/endpoint
```

**Passos:**
1. Acesse: https://portal.azure.com/
2. Navegue até **Azure Active Directory** > **App registrations**
3. Selecione o app com Client ID: `6e270dc7-1159-42c0-a4e8-dbc5a029ceb2`
4. Vá em **Authentication** > **Platform configurations**
5. Verifique se o Redirect URI está configurado corretamente
6. Verifique se o tipo está como **Web** (não SPA ou Mobile)

### 2. Verificar Permissões (API Permissions)

**Permissões necessárias:**
- `openid` (já incluído)
- `profile` (já incluído)
- `email` (já incluído)
- `User.Read` (pode ser necessário adicionar)

**Passos:**
1. No Azure Portal, vá em **API permissions**
2. Adicione **Microsoft Graph** > **Delegated permissions**
3. Adicione: `User.Read`, `email`, `profile`, `openid`
4. Clique em **Grant admin consent** se necessário

### 3. Verificar Client Secret

**Importante:**
- O Client Secret pode ter expirado
- Verifique se o secret no Keycloak corresponde ao secret no Azure Portal
- Se expirou, crie um novo secret e atualize no JSON

**Passos:**
1. No Azure Portal, vá em **Certificates & secrets**
2. Verifique se o secret está ativo
3. Se expirou, crie um novo e atualize no `assistenteexecutivo-realm.json`

### 4. Verificar Logs do Keycloak

**Verificar logs do container:**
```bash
docker logs keycloak | grep -i microsoft
docker logs keycloak | grep -i "502"
docker logs keycloak | grep -i error
```

**Verificar logs do Nginx (se aplicável):**
```bash
docker logs nginx | grep -i "502"
```

### 5. Verificar Configuração do Nginx/Proxy

O erro 502 pode ser causado por:
- Buffer muito pequeno para headers grandes
- Timeout muito curto
- Problemas com SSL/TLS

**Configuração recomendada do Nginx:**
```nginx
proxy_buffer_size 128k;
proxy_buffers 4 256k;
proxy_busy_buffers_size 256k;
proxy_read_timeout 300s;
proxy_connect_timeout 300s;
```

### 6. Verificar Configuração do Tenant

No JSON, o `tenant` está configurado como `"common"`. Isso permite:
- Contas Microsoft pessoais
- Contas corporativas de qualquer organização

**Alternativas:**
- `"organizations"` - apenas contas corporativas
- `"consumers"` - apenas contas pessoais
- `"<tenant-id>"` - apenas uma organização específica

### 7. Comparar com Google (que funciona)

**Diferenças entre Google e Microsoft:**
- Google não precisa de `tenant`
- Microsoft pode precisar de configurações adicionais

**Verificar se o Microsoft IdP foi criado corretamente:**
1. Acesse o Keycloak Admin Console
2. Vá em **Identity Providers**
3. Verifique se o Microsoft está listado e habilitado
4. Clique em **Microsoft** e verifique as configurações

### 8. Testar Manualmente

**Testar o endpoint diretamente:**
```bash
curl -v "https://keycloak.callback-local-cchagas.xyz/realms/assistenteexecutivo/broker/microsoft/endpoint"
```

**Verificar se o Keycloak está respondendo:**
```bash
curl -v "https://keycloak.callback-local-cchagas.xyz/realms/assistenteexecutivo"
```

### 9. Reaplicar Configuração

Se o Microsoft IdP foi criado manualmente antes da importação JSON, pode haver conflito:

1. **Deletar o Microsoft IdP manualmente:**
   - Keycloak Admin Console > Identity Providers > Microsoft > Delete

2. **Reiniciar o backend:**
   - A importação parcial recriará o IdP com as configurações corretas

### 10. Verificar Versão do Keycloak

Algumas versões do Keycloak têm bugs conhecidos com Microsoft IdP:
- Verifique se está usando uma versão estável
- Considere atualizar se estiver em versão antiga

## Configuração Atual no JSON

```json
{
  "alias": "microsoft",
  "providerId": "microsoft",
  "enabled": true,
  "trustEmail": true,
  "storeToken": false,
  "addReadTokenRoleOnCreate": false,
  "firstBrokerLoginFlowAlias": "first broker login",
  "config": {
    "clientId": "6e270dc7-1159-42c0-a4e8-dbc5a029ceb2",
    "clientSecret": "ygZ8Q~5MqVC6NIcfhyF7joSX_oa64iWW8tgHWcPS",
    "defaultScope": "openid profile email",
    "useJwksUrl": "true",
    "tenant": "common",
    "hideOnLoginPage": "false",
    "acceptsPromptNoneForwardFromClient": "false",
    "disableUserInfo": "false"
  }
}
```

## Erro Específico: "Could not obtain user profile from Microsoft Graph"

Se você está vendo este erro nos logs:
```
ERROR [org.keycloak.broker.oidc.AbstractOAuth2IdentityProvider] Failed to make identity provider oauth callback: 
Could not obtain user profile from Microsoft Graph
Error in Microsoft Graph API response. Payload: {"error":{"code":"UnknownError"...}}
```

### Causa
O Keycloak consegue fazer o callback do Microsoft, mas falha ao buscar informações do usuário na Microsoft Graph API.

### Solução

1. **Adicionar Permissão User.Read no Azure Portal:**
   - Acesse: https://portal.azure.com/
   - Vá em **Azure Active Directory** > **App registrations**
   - Selecione o app: `6e270dc7-1159-42c0-a4e8-dbc5a029ceb2`
   - Vá em **API permissions**
   - Clique em **Add a permission**
   - Selecione **Microsoft Graph** > **Delegated permissions**
   - Adicione: `User.Read`, `email`, `profile`, `openid`
   - Clique em **Grant admin consent** (importante!)

2. **Verificar Scope no JSON:**
   O scope deve incluir `User.Read`:
   ```json
   "defaultScope": "openid profile email User.Read"
   ```

3. **Reiniciar o Backend:**
   - A importação parcial reaplicará as configurações
   - O Microsoft IdP será atualizado com o novo scope

4. **Verificar se Admin Consent foi concedido:**
   - No Azure Portal, em **API permissions**
   - Verifique se há um check verde em "Status" para cada permissão
   - Se não houver, clique em **Grant admin consent for [sua organização]**

## Próximos Passos

1. ✅ Adicionar `User.Read` ao scope (já feito no JSON)
2. ⚠️ **IMPORTANTE**: Conceder Admin Consent no Azure Portal
3. Reiniciar o backend para reaplicar configurações
4. Testar novamente o login com Microsoft

