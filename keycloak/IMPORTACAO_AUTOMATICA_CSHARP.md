# Importação Automática de Realm via C# - Atualização Sempre que Inicia

## Visão Geral

O backend C# agora suporta importação automática do realm JSON a cada inicialização, atualizando o realm existente automaticamente.

## Como Funciona

1. **Na inicialização do backend**, o `KeycloakAdminProvisioner` verifica se há um arquivo JSON configurado
2. Se encontrado e habilitado, faz **importação parcial** do realm via API REST do Keycloak
3. O realm é **atualizado automaticamente** com todas as configurações do JSON
4. Se a importação falhar, usa o provisionamento manual tradicional como fallback

## Configuração

### 1. Habilitar Importação JSON

No `appsettings.Development.json` (ou `appsettings.json`):

```json
{
  "Keycloak": {
    "UseJsonImport": true,
    "RealmJsonPath": "../keycloak/realm-import/assistenteexecutivo-realm.json"
  }
}
```

**Configurações:**
- `UseJsonImport`: `true` para habilitar, `false` para desabilitar
- `RealmJsonPath`: Caminho relativo ou absoluto para o arquivo JSON do realm

### 2. Caminhos Relativos

O caminho é relativo ao diretório de execução do backend. Exemplos:

```json
// Relativo ao diretório de execução
"RealmJsonPath": "../keycloak/realm-import/assistenteexecutivo-realm.json"

// Absoluto
"RealmJsonPath": "C:/Projects/AssistenteExecutivo/keycloak/realm-import/assistenteexecutivo-realm.json"

// No mesmo diretório do appsettings
"RealmJsonPath": "realm-import/assistenteexecutivo-realm.json"
```

### 3. Docker/Container

Se rodando em container, monte o volume:

```yaml
volumes:
  - ./keycloak/realm-import:/app/realm-import:ro
```

E configure:
```json
"RealmJsonPath": "/app/realm-import/assistenteexecutivo-realm.json"
```

## Fluxo de Execução

```
Backend Inicia
    ↓
KeycloakAdminProvisioner.StartAsync()
    ↓
Verifica: UseJsonImport = true?
    ↓ SIM
Verifica: Arquivo JSON existe?
    ↓ SIM
Importa via ImportRealmFromJsonAsync()
    ↓
Sucesso?
    ↓ SIM
Atualiza configurações dinâmicas (frontendUrl, etc.)
    ↓
FIM (pula provisionamento manual)
    ↓
NÃO / Arquivo não existe
    ↓
Provisionamento Manual Tradicional
    ↓
(Cria realm, clients, roles, users, etc.)
```

## Vantagens

✅ **Atualização Automática**: Sempre que o backend inicia, o realm é atualizado do JSON
✅ **Idempotente**: Pode rodar múltiplas vezes sem problemas
✅ **Fallback Seguro**: Se o JSON falhar, usa provisionamento manual
✅ **Versionamento**: JSON pode ser versionado no Git
✅ **Declarativo**: Toda configuração em um único arquivo

## Logs

Quando a importação JSON é usada, você verá:

```
[Information] Tentando importar realm assistenteexecutivo do arquivo JSON: ../keycloak/realm-import/assistenteexecutivo-realm.json
[Information] Realm assistenteexecutivo já existe. Fazendo importação parcial (OVERWRITE=True)...
[Information] ✓ Realm assistenteexecutivo importado/atualizado com sucesso via importação parcial
[Information] ✓ Realm assistenteexecutivo importado/atualizado com sucesso do JSON. Pulando provisionamento manual.
[Information] Configurações dinâmicas (frontendUrl, etc.) atualizadas no realm assistenteexecutivo
```

Se falhar:

```
[Warning] Falha ao importar realm do JSON. Continuando com provisionamento manual...
[Information] Iniciando criação do client assistenteexecutivo-app no realm assistenteexecutivo
...
```

## Desabilitar Importação JSON

Para voltar ao provisionamento manual tradicional:

```json
{
  "Keycloak": {
    "UseJsonImport": false
  }
}
```

Ou simplesmente remova a configuração `UseJsonImport`.

## Troubleshooting

### Arquivo JSON não encontrado

```
[Warning] Arquivo JSON de realm não encontrado: ../keycloak/realm-import/assistenteexecutivo-realm.json
```

**Solução**: Verifique o caminho em `RealmJsonPath`. Use caminho absoluto se necessário.

### Erro de parse do JSON

```
[Error] Erro ao fazer parse do JSON do realm assistenteexecutivo
```

**Solução**: Valide o JSON com um validador online ou `python -m json.tool arquivo.json`

### Keycloak não está pronto

```
[Warning] Importação parcial do realm assistenteexecutivo retornou código 503
```

**Solução**: O Keycloak pode não estar totalmente inicializado. O provisionamento tentará novamente na próxima inicialização.

### Token de admin inválido

```
[Error] Erro ao obter token de admin do Keycloak
```

**Solução**: Verifique `Keycloak:AdminUsername` e `Keycloak:AdminPassword` no appsettings.

## Comparação: JSON vs Manual

| Aspecto | Importação JSON | Provisionamento Manual |
|---------|----------------|------------------------|
| **Atualização** | Automática a cada inicialização | Apenas cria/atualiza se necessário |
| **Configuração** | Declarativa (JSON) | Imperativa (C#) |
| **Versionamento** | ✅ Fácil (Git) | ⚠️ Código C# |
| **Flexibilidade** | ⚠️ Estático | ✅ Dinâmico (lógica) |
| **Manutenção** | ✅ Mais simples | ⚠️ Mais complexo |

## Recomendação

- **Desenvolvimento**: Use importação JSON (`UseJsonImport: true`)
- **Produção**: Considere desabilitar ou usar apenas para inicialização única

## Referências

- [Keycloak Partial Import API](https://www.keycloak.org/docs-api/latest/rest-api/index.html#_partialimport)
- Arquivo JSON: `keycloak/realm-import/assistenteexecutivo-realm.json`

