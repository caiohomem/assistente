# Importação Parcial do Realm - Atualizar Configurações Existentes

## Visão Geral

O Keycloak oferece dois tipos de importação:

1. **Importação Completa (`--import-realm`)**: Funciona apenas na **primeira inicialização** quando o realm não existe. Se o realm já existir, a importação é ignorada.

2. **Importação Parcial**: Permite atualizar um realm existente com novas configurações, escolhendo como lidar com recursos duplicados.

## Quando Usar Importação Parcial

Use importação parcial quando:
- ✅ O realm já existe e você quer atualizar configurações
- ✅ Você modificou o arquivo JSON e quer aplicar as mudanças
- ✅ Você quer sincronizar configurações do JSON com o realm existente

## Métodos de Importação Parcial

### Método 1: Via Admin Console (Recomendado)

1. Acesse o Keycloak Admin Console: `https://keycloak.callback-local-cchagas.xyz`
2. Faça login como admin
3. Selecione o realm `assistenteexecutivo`
4. Vá em **Realm Settings** (Configurações do Realm)
5. No canto superior direito, clique no menu **Ações** (Actions)
6. Selecione **Partial Import** (Importação Parcial)
7. Clique em **Select File** e escolha `assistenteexecutivo-realm.json`
8. Escolha a ação para recursos existentes:
   - **Fail**: Aborta se encontrar duplicados (mais seguro)
   - **Skip**: Ignora recursos duplicados (mantém existentes)
   - **Overwrite**: Substitui recursos existentes (atualiza)
9. Clique em **Import**

### Método 2: Via Admin CLI (kcadm.sh)

```bash
# 1. Autenticar no Keycloak
docker exec -it keycloak /opt/keycloak/bin/kcadm.sh config credentials \
  --server http://localhost:8080 \
  --realm master \
  --user admin \
  --password admin

# 2. Fazer importação parcial (overwrite)
docker exec -it keycloak /opt/keycloak/bin/kcadm.sh create partialImport \
  -r assistenteexecutivo \
  -s ifResourceExists=OVERWRITE \
  -o -f /opt/keycloak/data/import/assistenteexecutivo-realm.json
```

Opções para `ifResourceExists`:
- `FAIL`: Falha se encontrar recursos duplicados
- `SKIP`: Ignora recursos duplicados
- `OVERWRITE`: Substitui recursos existentes

### Método 3: Via API REST

```bash
# 1. Obter token de admin
TOKEN=$(curl -X POST "https://keycloak.callback-local-cchagas.xyz/realms/master/protocol/openid-connect/token" \
  -H "Content-Type: application/x-www-form-urlencoded" \
  -d "username=admin" \
  -d "password=admin" \
  -d "grant_type=password" \
  -d "client_id=admin-cli" | jq -r '.access_token')

# 2. Fazer importação parcial
curl -X POST "https://keycloak.callback-local-cchagas.xyz/admin/realms/assistenteexecutivo/partialImport" \
  -H "Authorization: Bearer $TOKEN" \
  -H "Content-Type: application/json" \
  -d '{
    "ifResourceExists": "OVERWRITE",
    "realm": <conteúdo do JSON>
  }'
```

## O Que é Atualizado

O arquivo `assistenteexecutivo-realm.json` contém:

- ✅ **Realm Settings**: registrationAllowed, resetPasswordAllowed, rememberMe, themes
- ✅ **Clients**: assistenteexecutivo-app (redirectUris, webOrigins, atributos)
- ✅ **Roles**: admin, user, viewer
- ✅ **Users**: Usuários dev/teste (admin, user, viewer)
- ✅ **Identity Providers**: Google e Microsoft
- ✅ **Identity Provider Mappers**: Mapeamento de atributos

## Estratégias de Atualização

### Atualização Completa (OVERWRITE)
```bash
ifResourceExists=OVERWRITE
```
- **Quando usar**: Quando você quer garantir que o realm está exatamente como no JSON
- **Risco**: Pode sobrescrever configurações manuais feitas no console
- **Recomendado para**: Ambientes de desenvolvimento, sincronização de configurações

### Atualização Incremental (SKIP)
```bash
ifResourceExists=SKIP
```
- **Quando usar**: Quando você quer adicionar apenas recursos novos
- **Risco**: Configurações antigas podem não ser atualizadas
- **Recomendado para**: Adicionar novos recursos sem alterar existentes

### Validação (FAIL)
```bash
ifResourceExists=FAIL
```
- **Quando usar**: Para validar se o JSON não tem conflitos
- **Risco**: Não atualiza nada se houver duplicados
- **Recomendado para**: Testes e validação antes de aplicar mudanças

## Fluxo Recomendado

### Primeira Vez (Realm Não Existe)
1. Coloque o JSON em `keycloak/realm-import/`
2. Inicie o Keycloak
3. O realm será criado automaticamente

### Atualizações Subsequentes (Realm Já Existe)
1. Atualize o arquivo JSON com as mudanças desejadas
2. Use importação parcial via Admin Console ou CLI
3. Escolha `OVERWRITE` para atualizar tudo ou `SKIP` para apenas adicionar

## Verificação Após Importação

```bash
# Verificar se o realm foi atualizado
docker exec -it keycloak /opt/keycloak/bin/kcadm.sh get realms/assistenteexecutivo

# Verificar clients
docker exec -it keycloak /opt/keycloak/bin/kcadm.sh get clients -r assistenteexecutivo

# Verificar roles
docker exec -it keycloak /opt/keycloak/bin/kcadm.sh get roles -r assistenteexecutivo

# Verificar identity providers
docker exec -it keycloak /opt/keycloak/bin/kcadm.sh get identity-provider/instances -r assistenteexecutivo
```

## Troubleshooting

### Erro: "Realm already exists"
- **Causa**: Tentando usar `--import-realm` em realm existente
- **Solução**: Use importação parcial em vez de importação completa

### Erro: "Resource already exists"
- **Causa**: Recurso já existe e `ifResourceExists=FAIL`
- **Solução**: Use `SKIP` ou `OVERWRITE`

### Configurações não foram atualizadas
- **Causa**: Usou `SKIP` e recursos já existiam
- **Solução**: Use `OVERWRITE` para forçar atualização

### Frontend URL não foi atualizado
- **Causa**: Frontend URL é dinâmico e não deve estar no JSON
- **Solução**: Configure via Admin Console ou API após importação

## Notas Importantes

⚠️ **Frontend URL**: O `frontendUrl` do realm é configurado dinamicamente pelo backend C# e não deve estar no JSON de importação.

⚠️ **Secrets**: Credenciais de Identity Providers estão no JSON. Em produção, considere usar variáveis de ambiente.

⚠️ **Backup**: Sempre faça backup do realm antes de importação parcial com `OVERWRITE`:
```bash
docker exec -it keycloak /opt/keycloak/bin/kcadm.sh get realms/assistenteexecutivo > backup-realm.json
```

## Referências

- [Keycloak Partial Import Documentation](https://www.keycloak.org/server/importExport)
- [Keycloak Admin CLI](https://www.keycloak.org/docs/latest/server_admin/#_admin_cli)

