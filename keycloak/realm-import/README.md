# Importação de Realm - Identity Providers

Este diretório contém o arquivo JSON de configuração do realm `assistenteexecutivo` com os Identity Providers configurados.

## Identity Providers Configurados

### Google
- **Client ID**: `533937474214-ar1c0gr7onm0lvmvrj1cpid1jh718atu.apps.googleusercontent.com`
- **Redirect URI**: `https://keycloak.callback-local-cchagas.xyz/realms/assistenteexecutivo/broker/google/endpoint`

### Microsoft
- **Client ID**: `6e270dc7-1159-42c0-a4e8-dbc5a029ceb2`
- **Client Secret**: `ygZ8Q~5MqVC6NIcfhyF7joSX_oa64iWW8tgHWcPS`
- **Redirect URI**: `https://keycloak.callback-local-cchagas.xyz/realms/assistenteexecutivo/broker/microsoft/endpoint`

## Configuração dos Redirect URIs

### Google Cloud Console
1. Acesse: https://console.cloud.google.com/apis/credentials
2. Selecione o OAuth 2.0 Client ID: `533937474214-ar1c0gr7onm0lvmvrj1cpid1jh718atu`
3. Adicione o Redirect URI:
   ```
   https://keycloak.callback-local-cchagas.xyz/realms/assistenteexecutivo/broker/google/endpoint
   ```

### Microsoft Azure Portal
1. Acesse: https://portal.azure.com/
2. Navegue até **Azure Active Directory** > **App registrations**
3. Selecione o app com Client ID: `6e270dc7-1159-42c0-a4e8-dbc5a029ceb2`
4. Vá em **Authentication** > **Platform configurations**
5. Adicione o Redirect URI:
   ```
   https://keycloak.callback-local-cchagas.xyz/realms/assistenteexecutivo/broker/microsoft/endpoint
   ```

## Mappers Configurados

Ambos os providers têm mappers configurados para sincronizar:
- **email** → `user.attribute.email`
- **given_name** → `user.attribute.firstName`
- **family_name** → `user.attribute.lastName`
- **name** → `user.attribute.name`

Todos os mappers usam `syncMode: INHERIT` para sincronização automática.

## Importação Automática (Primeira Vez)

O arquivo `assistenteexecutivo-realm.json` será importado automaticamente na **primeira inicialização** do Keycloak se estiver neste diretório e o realm não existir.

⚠️ **Importante**: A importação automática (`--import-realm`) só funciona na primeira vez. Se o realm já existir, a importação é ignorada.

## Atualizar Realm Existente

Para atualizar um realm existente com novas configurações, use **importação parcial**. Veja `IMPORTACAO_PARCIAL.md` para detalhes.

### Via Admin Console (Mais Fácil)
1. Acesse Keycloak Admin Console
2. Realm Settings → Actions → Partial Import
3. Selecione o arquivo JSON
4. Escolha `OVERWRITE` para atualizar tudo
5. Clique em Import

### Via CLI
```bash
docker exec -it keycloak /opt/keycloak/bin/kcadm.sh create partialImport \
  -r assistenteexecutivo \
  -s ifResourceExists=OVERWRITE \
  -o -f /opt/keycloak/data/import/assistenteexecutivo-realm.json
```

## Notas de Segurança

⚠️ **IMPORTANTE**: Este arquivo contém credenciais sensíveis. Certifique-se de:
- Não commitar este arquivo em repositórios públicos
- Usar variáveis de ambiente em produção
- Rotacionar secrets regularmente

Para produção, considere usar variáveis de ambiente ou um sistema de gerenciamento de secrets.

