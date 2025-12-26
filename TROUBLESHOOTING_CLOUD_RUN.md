# Troubleshooting - Cloud Run

## Erro: "The user-provided container failed to start and listen on the port"

Este erro geralmente ocorre quando:
1. A aplicação não consegue iniciar (crash na inicialização)
2. A aplicação não está escutando na porta correta
3. A aplicação demora muito para iniciar (timeout)

### ✅ Solução 1: Verificar Variáveis de Ambiente

A aplicação **precisa** das seguintes variáveis de ambiente para iniciar:

```bash
# OBRIGATÓRIAS
ConnectionStrings__DefaultConnection="Host=...;Database=...;Username=...;Password=...;SSL Mode=Require;"
Keycloak__BaseUrl="http://keycloak:8080"  # ou URL pública
Keycloak__PublicBaseUrl="https://auth.seu-dominio.com"
Keycloak__Realm="assistenteexecutivo"
Keycloak__AdminUsername="admin"
Keycloak__AdminPassword="senha"
Keycloak__ClientId="assistenteexecutivo-app"
ASPNETCORE_ENVIRONMENT="Production"
```

**Como verificar:**
1. Acesse o Cloud Run no Console
2. Vá para o serviço `assistente-api`
3. Clique em **EDIT & DEPLOY NEW REVISION**
4. Vá para **Variables & Secrets**
5. Verifique se todas as variáveis obrigatórias estão configuradas

### ✅ Solução 2: Verificar Logs

Os logs mostram o erro exato:

```bash
# Via CLI
gcloud run services logs read assistente-api --region us-central1 --limit 50

# Ou no Console
# Cloud Run > assistente-api > Logs
```

Procure por:
- `ConnectionString 'DefaultConnection' não configurada`
- `Keycloak:PublicBaseUrl ou Keycloak:BaseUrl deve estar configurado`
- Erros de migração do banco de dados
- Erros de conexão com Keycloak

### ✅ Solução 3: Aumentar Timeout

Se a aplicação demora para iniciar (migrations, seed, etc):

```bash
gcloud run services update assistente-api \
  --region us-central1 \
  --timeout 300 \
  --cpu 1 \
  --memory 512Mi
```

### ✅ Solução 4: Testar Localmente com Docker

Teste a imagem Docker localmente antes de fazer deploy:

```bash
# Build local
docker build -f backend/Dockerfile -t assistente-api:test backend

# Rodar com variáveis de ambiente
docker run -p 8080:8080 \
  -e ConnectionStrings__DefaultConnection="..." \
  -e Keycloak__BaseUrl="..." \
  -e Keycloak__PublicBaseUrl="..." \
  -e ASPNETCORE_ENVIRONMENT="Production" \
  assistente-api:test

# Testar se está respondendo
curl http://localhost:8080/health  # ou endpoint que você tenha
```

### ✅ Solução 5: Verificar Porta

O Cloud Run define automaticamente a variável `PORT`. O Dockerfile já está configurado para usar:

```dockerfile
ENV ASPNETCORE_URLS=http://+:${PORT:-8080}
```

Isso significa:
- Se `PORT` estiver definido, usa esse valor
- Se não, usa 8080 como padrão

**Não precisa mudar a porta!** O Cloud Run sempre usa 8080 por padrão.

### ✅ Solução 6: Configurar Variáveis no Deploy

Adicione as variáveis diretamente no `cloudbuild.yaml`:

```yaml
gcloud run deploy ${_CLOUD_RUN_SERVICE_API} \
  --image gcr.io/$PROJECT_ID/assistente-api:$COMMIT_SHA \
  --region ${_CLOUD_RUN_REGION} \
  --platform managed \
  --port 8080 \
  --set-env-vars \
    ConnectionStrings__DefaultConnection="${_DB_CONNECTION_STRING}",\
    Keycloak__BaseUrl="${_KEYCLOAK_BASE_URL}",\
    Keycloak__PublicBaseUrl="${_KEYCLOAK_PUBLIC_URL}",\
    Keycloak__Realm="assistenteexecutivo",\
    Keycloak__AdminUsername="${_KEYCLOAK_ADMIN_USER}",\
    Keycloak__AdminPassword="${_KEYCLOAK_ADMIN_PASS}",\
    Keycloak__ClientId="assistenteexecutivo-app",\
    ASPNETCORE_ENVIRONMENT="Production"
```

E adicione as substituições no trigger:
- `_DB_CONNECTION_STRING`
- `_KEYCLOAK_BASE_URL`
- `_KEYCLOAK_PUBLIC_URL`
- `_KEYCLOAK_ADMIN_USER`
- `_KEYCLOAK_ADMIN_PASS`

### ✅ Solução 7: Usar Secret Manager (Recomendado)

Para informações sensíveis:

```bash
# Criar secrets
echo -n "sua-connection-string" | gcloud secrets create db-connection-string --data-file=-
echo -n "senha-admin" | gcloud secrets create keycloak-admin-password --data-file=-

# Dar permissão ao Cloud Run
PROJECT_NUMBER=$(gcloud projects describe $PROJECT_ID --format="value(projectNumber)")
gcloud secrets add-iam-policy-binding db-connection-string \
  --member="serviceAccount:${PROJECT_NUMBER}-compute@developer.gserviceaccount.com" \
  --role="roles/secretmanager.secretAccessor"

gcloud secrets add-iam-policy-binding keycloak-admin-password \
  --member="serviceAccount:${PROJECT_NUMBER}-compute@developer.gserviceaccount.com" \
  --role="roles/secretmanager.secretAccessor"

# Configurar no Cloud Run
gcloud run services update assistente-api \
  --region us-central1 \
  --update-secrets \
    ConnectionStrings__DefaultConnection=db-connection-string:latest,\
    Keycloak__AdminPassword=keycloak-admin-password:latest
```

## Checklist de Verificação

Antes de fazer deploy, verifique:

- [ ] Connection string do banco configurada
- [ ] Keycloak BaseUrl e PublicBaseUrl configurados
- [ ] Keycloak AdminUsername e AdminPassword configurados
- [ ] Keycloak Realm e ClientId configurados
- [ ] ASPNETCORE_ENVIRONMENT definido como "Production"
- [ ] Timeout configurado (mínimo 300s para primeira inicialização)
- [ ] Memória suficiente (mínimo 512Mi)
- [ ] Logs verificados para erros específicos

## Erros Comuns

### "ConnectionString 'DefaultConnection' não configurada"
**Solução:** Configure `ConnectionStrings__DefaultConnection` no Cloud Run

### "Keycloak:PublicBaseUrl ou Keycloak:BaseUrl deve estar configurado"
**Solução:** Configure `Keycloak__BaseUrl` ou `Keycloak__PublicBaseUrl`

### "Unable to connect to database"
**Solução:** Verifique se o Cloud Run tem acesso ao banco (IP whitelist, VPC, etc)

### "Keycloak connection failed"
**Solução:** Verifique se o Keycloak está acessível e as credenciais estão corretas

### Timeout na inicialização
**Solução:** Aumente o timeout e verifique se há processos demorados (migrations, seed)

## Próximos Passos

1. Verifique os logs do Cloud Run
2. Configure as variáveis de ambiente obrigatórias
3. Teste localmente com Docker
4. Faça deploy novamente

Para mais informações, consulte [ENV_VARIABLES.md](./ENV_VARIABLES.md)

