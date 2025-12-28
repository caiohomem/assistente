# Provisionamento Simplificado do Keycloak

## Visão Geral

O Keycloak 26.0+ oferece provisionamento automático muito mais simples que scripts SQL manuais. Esta documentação explica as formas mais simples de configurar o Keycloak.

## Método 1: Provisionamento Automático (Recomendado)

### Admin Inicial

O Keycloak cria automaticamente o usuário admin na primeira inicialização usando as variáveis de ambiente:

```yaml
KC_BOOTSTRAP_ADMIN_USERNAME: admin
KC_BOOTSTRAP_ADMIN_PASSWORD: admin
```

**Não é necessário nenhum script SQL!** O Keycloak faz isso automaticamente.

### Realm e Configurações

O backend C# já tem um serviço de provisionamento automático (`KeycloakAdminProvisioner`) que:
- Cria o realm `assistenteexecutivo`
- Cria clients necessários
- Configura roles
- Configura Identity Providers (Google, etc.)
- Aplica o theme customizado

Este serviço roda automaticamente no startup do backend (apenas em DEV/HML).

## Método 2: Importação de Realm via JSON (Alternativa)

Se preferir configurar tudo via arquivo JSON (mais declarativo):

### 1. Criar arquivo de realm

Crie um arquivo JSON no diretório `keycloak/realm-import/` (ex: `assistenteexecutivo-realm.json`).

Exemplo básico já está em `realm-import/assistenteexecutivo-realm.json`.

### 2. O Keycloak importa automaticamente

O script `start-keycloak.sh` verifica se há arquivos `.json` em `/opt/keycloak/data/import` e os importa automaticamente na primeira inicialização.

### 3. Estrutura do arquivo JSON

```json
{
  "realm": "assistenteexecutivo",
  "enabled": true,
  "displayName": "Assistente Executivo",
  "clients": [...],
  "roles": {...},
  "users": [...],
  "identityProviders": [...],
  "identityProviderMappers": [...]
}
```

O arquivo `assistenteexecutivo-realm.json` já inclui:
- ✅ Identity Provider do Google configurado
- ✅ Identity Provider da Microsoft configurado
- ✅ Mappers para sincronização de atributos (email, nome, sobrenome)

### Exportar realm existente

Para exportar um realm existente e usar como base:

```bash
# Via Admin CLI (dentro do container)
docker exec -it keycloak /opt/keycloak/bin/kc.sh export --realm assistenteexecutivo --file /tmp/realm.json

# Copiar para fora
docker cp keycloak:/tmp/realm.json ./realm-import/assistenteexecutivo-realm.json
```

## Comparação de Métodos

| Método | Complexidade | Quando Usar |
|--------|-------------|-------------|
| **Provisionamento Automático (Backend C#)** | ⭐⭐ Média | Desenvolvimento, quando precisa de lógica dinâmica |
| **Importação JSON** | ⭐ Baixa | Quando quer configuração declarativa, versionamento fácil |
| **Scripts SQL** | ⭐⭐⭐ Alta | ❌ **NÃO RECOMENDADO** - Use apenas em emergências |

## Removendo Scripts SQL Antigos

Os scripts SQL em `keycloak/*.sql` não são mais necessários para o provisionamento inicial. Eles podem ser mantidos apenas para:
- Recuperação de emergência
- Reset manual de senha (se necessário)

## Fluxo Recomendado

1. **Primeira inicialização:**
   - Keycloak cria admin automaticamente via `KC_BOOTSTRAP_ADMIN_USERNAME`
   - Se houver arquivos em `realm-import/`, eles são importados
   - Backend inicia e executa `KeycloakAdminProvisioner` (DEV/HML)

2. **Subsequentes:**
   - Keycloak usa configuração existente do banco
   - Backend verifica e atualiza apenas o necessário

## Troubleshooting

### Admin não foi criado

1. Verifique se `KC_BOOTSTRAP_ADMIN_USERNAME` está configurado
2. Verifique logs: `docker logs keycloak`
3. O admin só é criado na **primeira inicialização** de um banco vazio

### Realm não foi criado

1. Verifique logs do backend: `KeycloakAdminProvisioner`
2. Verifique se o backend consegue acessar o Keycloak
3. Verifique configuração em `appsettings.json`

### Importação JSON não funcionou

1. Verifique se o arquivo está em `keycloak/realm-import/`
2. Verifique se o arquivo é JSON válido
3. Verifique logs: `docker logs keycloak | grep -i import`

## Identity Providers

O arquivo de realm inclui configuração para:
- **Google**: Login com conta Google
- **Microsoft**: Login com conta Microsoft/Azure AD

⚠️ **Importante**: Certifique-se de configurar os Redirect URIs corretos nos consoles do Google e Microsoft. Veja `realm-import/README.md` para detalhes.

## Referências

- [Keycloak Documentation - Import/Export](https://www.keycloak.org/docs/latest/server_admin/#_export_import)
- [Keycloak Bootstrap Admin](https://www.keycloak.org/docs/latest/server_admin/#_bootstrap_admin)
- [Keycloak Identity Providers](https://www.keycloak.org/docs/latest/server_admin/#_identity_broker)

