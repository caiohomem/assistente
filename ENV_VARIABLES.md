# Variáveis de Ambiente - Assistente Executivo

Este documento lista todas as variáveis de ambiente necessárias para cada serviço.

## 📋 Índice

- [API (Backend)](#api-backend)
- [Web (Frontend)](#web-frontend)
- [Configuração no Cloud Run](#configuração-no-cloud-run)

---

## 🔧 API (Backend)

### Variáveis Obrigatórias

#### Banco de Dados
```bash
ConnectionStrings__DefaultConnection="Host=...;Database=...;Username=...;Password=...;SSL Mode=Require;"
```
- **Descrição**: Connection string do PostgreSQL ou SQL Server
- **Formato**: 
  - PostgreSQL: `Host=...;Database=...;Username=...;Password=...;SSL Mode=Require;`
  - SQL Server: `Server=...;Database=...;User Id=...;Password=...;`

#### Redis (Sessão BFF / OAuth state)
```bash
ConnectionStrings__Redis="10.0.0.5:6379"
```
- **Descrição**: Redis para `IDistributedCache` (necessário em Cloud Run quando usando PostgreSQL, para não perder sessão/state entre instâncias)
- **Exemplo (com senha/SSL)**: `host:6379,password=...,ssl=True,abortConnect=False`

#### Keycloak (Autenticação)
```bash
Keycloak__BaseUrl="http://localhost:8080"                    # URL interna do Keycloak
Keycloak__PublicBaseUrl="https://auth.seu-dominio.com"        # URL pública do Keycloak (HTTPS)
Keycloak__Realm="assistenteexecutivo"                         # Nome do realm
Keycloak__RealmName="Assistente Executivo"                   # Nome amigável do realm
Keycloak__AdminRealm="master"                                 # Realm do admin
Keycloak__AdminClientId="admin-cli"                           # Client ID do admin
Keycloak__AdminClientSecret=""                                # Client Secret do admin (se necessário)
Keycloak__AdminUsername="admin"                                # Username do admin
Keycloak__AdminPassword="admin"                               # Password do admin
Keycloak__ClientId="assistenteexecutivo-app"                  # Client ID da aplicação
Keycloak__ClientSecret=""                                     # Client Secret (se necessário)
Keycloak__ThemeName="assistenteexecutivo"                     # Nome do tema customizado
Keycloak__GoogleRedirectUri="https://auth.seu-dominio.com/realms/assistenteexecutivo/broker/google/endpoint"
Keycloak__Google__ClientId="..."                              # Google OAuth Client ID
Keycloak__Google__ClientSecret="..."                          # Google OAuth Client Secret
```

#### URLs da Aplicação
```bash
Api__BaseUrl="http://localhost:5239"                         # URL interna da API
Api__PublicBaseUrl="https://assistente-api-xxx.run.app"       # URL pública da API (HTTPS) - OBRIGATÓRIA para OAuth
Frontend__BaseUrl="http://localhost:3000"                     # URL interna do frontend
Frontend__PublicBaseUrl="https://assistente.seu-dominio.com"   # URL pública do frontend (HTTPS)
Frontend__CorsOrigins="https://assistente.seu-dominio.com,https://assistente-web-xxx.run.app"
```

**⚠️ Importante:** `Api__PublicBaseUrl` é **obrigatória** para OAuth funcionar. Ela é usada para:
- Registrar o redirect URI válido no Keycloak (`{Api__PublicBaseUrl}/auth/oauth-callback`)
- Construir URLs corretas para callbacks OAuth

Sem essa variável, você receberá o erro: `Invalid parameter: redirect_uri`

#### Ambiente
```bash
ASPNETCORE_ENVIRONMENT="Production"                           # Development, Staging, Production
```

### Variáveis Opcionais

#### Email (SMTP)
```bash
Email__Smtp__Host="smtp.gmail.com"                          # Servidor SMTP
Email__Smtp__Port="587"                                      # Porta SMTP (587 para TLS, 465 para SSL)
Email__Smtp__User="noreply@seu-dominio.com"                 # Username SMTP
Email__Smtp__Password="..."                                 # Password SMTP
Email__Smtp__From="noreply@seu-dominio.com"                  # Email remetente
Email__Smtp__FromName="Assistente Executivo"                # Nome do remetente
Email__Smtp__EnableSsl="true"                               # true/false
```

#### OCR (Reconhecimento de Texto)
```bash
# Provider: Stub, PaddleOcr, OpenAI, Ollama, Azure, GoogleCloud, Aws
Ocr__Provider="PaddleOcr"

# PaddleOCR
Ocr__PaddleOcr__BaseUrl="http://localhost:8000"
Ocr__PaddleOcr__Lang="pt"

# Azure Computer Vision
Ocr__Azure__Endpoint="https://...cognitiveservices.azure.com/"
Ocr__Azure__ApiKey="..."
Ocr__Azure__ApiVersion="2024-02-01"

# Google Cloud Vision
Ocr__GoogleCloud__ProjectId="..."
Ocr__GoogleCloud__CredentialsJson="..."                      # JSON completo das credenciais

# AWS Textract
Ocr__Aws__Region="us-east-1"
Ocr__Aws__AccessKeyId="..."
Ocr__Aws__SecretAccessKey="..."
```

#### Ollama (LLM e OCR)
```bash
Ollama__BaseUrl="http://localhost:11434"

# OCR com Ollama
Ollama__Ocr__Model="llava:latest"
Ollama__Ocr__Temperature="0.0"
Ollama__Ocr__MaxTokens="1000"

# LLM com Ollama
Ollama__LLM__Provider="Ollama"                              # Ollama ou Stub
Ollama__LLM__Model="qwen2.5:7b"
Ollama__LLM__Temperature="0.3"
Ollama__LLM__MaxTokens="2000"
```

#### Whisper (Transcrição de Áudio)
```bash
Whisper__Provider="Ollama"                                  # Ollama, OpenAI ou Stub
Whisper__Model="whisper"
Whisper__Language="pt"
Whisper__ApiUrl="http://localhost:8000"
```

#### OpenAI (OCR, Speech-to-Text, LLM e Text-to-Speech)
```bash
OpenAI__ApiKey="sk-..."                                     # API Key da OpenAI (obrigatório)
OpenAI__OrganizationId=""                                   # Organization ID (opcional)

# OCR com OpenAI Vision
Ocr__Provider="OpenAI"                                      # OpenAI, PaddleOcr, Ollama, Stub
OpenAI__Ocr__Model="gpt-4o-mini"                            # gpt-4o-mini ou gpt-4o
OpenAI__Ocr__Temperature="0.0"
OpenAI__Ocr__MaxTokens="500"

# Speech-to-Text com OpenAI Whisper
Whisper__Provider="OpenAI"                                  # OpenAI, Ollama ou Stub
OpenAI__SpeechToText__Model="whisper-1"
OpenAI__SpeechToText__Language="pt"

# LLM com OpenAI
Ollama__LLM__Provider="OpenAI"                             # OpenAI, Ollama ou Stub
OpenAI__LLM__Model="gpt-4o-mini"                          # gpt-4o-mini ou gpt-4o
OpenAI__LLM__Temperature="0.3"
OpenAI__LLM__MaxTokens="2000"

# Text-to-Speech com OpenAI
TextToSpeech__Provider="OpenAI"                            # OpenAI ou Stub
OpenAI__TextToSpeech__Model="tts-1"                        # tts-1 ou tts-1-hd
OpenAI__TextToSpeech__Voice="nova"                         # alloy, echo, fable, onyx, nova, shimmer
OpenAI__TextToSpeech__Format="mp3"                          # mp3, opus, aac, flac
OpenAI__TextToSpeech__Enabled="true"                       # true/false
```

#### Logging
```bash
# Configurado automaticamente via Serilog
# Logs vão para console e (se SQL Server) para tabela Logs
```

---

## 🌐 Web (Frontend)

### Variáveis Obrigatórias

#### API Base URL
```bash
NEXT_PUBLIC_API_BASE_URL="https://assistente-api.seu-dominio.com"
```
- **Descrição**: URL pública da API/BFF
- **Importante**: Deve começar com `https://` em produção
- **Nota**: Variáveis `NEXT_PUBLIC_* são expostas ao cliente

### Variáveis Opcionais

O Next.js também suporta outras variáveis de ambiente, mas atualmente apenas `NEXT_PUBLIC_API_BASE_URL` é utilizada.

---

## ☁️ Configuração no Cloud Run

### Como Configurar Variáveis de Ambiente

#### Opção 1: Via cloudbuild.yaml (Recomendado)

As variáveis são configuradas automaticamente durante o deploy. Atualmente, apenas a web tem variáveis configuradas:

```yaml
# Web
--set-env-vars NEXT_PUBLIC_API_BASE_URL=${_NEXT_PUBLIC_API_BASE_URL}

# API - Adicione aqui as variáveis necessárias
--set-env-vars ConnectionStrings__DefaultConnection=...,Keycloak__BaseUrl=...
```

#### Opção 2: Via Console do Google Cloud

1. Acesse **Cloud Run** no Console do Google Cloud
2. Selecione o serviço (ex: `assistente-api`)
3. Clique em **EDIT & DEPLOY NEW REVISION**
4. Vá para a aba **Variables & Secrets**
5. Adicione as variáveis necessárias

#### Opção 3: Via gcloud CLI

```bash
# API
gcloud run services update assistente-api \
  --region us-central1 \
  --update-env-vars \
    ConnectionStrings__DefaultConnection="...",\
    Keycloak__BaseUrl="...",\
    Keycloak__PublicBaseUrl="...",\
    ASPNETCORE_ENVIRONMENT="Production"

# Web
gcloud run services update assistente-web \
  --region us-central1 \
  --update-env-vars \
    NEXT_PUBLIC_API_BASE_URL="https://assistente-api-xxx.run.app"
```

#### Opção 4: Usar Secret Manager (Recomendado para Sensíveis)

Para informações sensíveis (senhas, tokens, etc.), use o Secret Manager:

```bash
# Criar secret
echo -n "sua-connection-string" | gcloud secrets create db-connection-string --data-file=-

# Dar permissão ao Cloud Run
gcloud secrets add-iam-policy-binding db-connection-string \
  --member="serviceAccount:PROJECT_NUMBER-compute@developer.gserviceaccount.com" \
  --role="roles/secretmanager.secretAccessor"

# Configurar no Cloud Run
gcloud run services update assistente-api \
  --region us-central1 \
  --update-secrets ConnectionStrings__DefaultConnection=db-connection-string:latest
```

---

## 📝 Exemplo Completo de Configuração

### API - Variáveis Mínimas para Produção

```bash
# Banco de Dados
ConnectionStrings__DefaultConnection="Host=ep-xxx-pooler.region.aws.neon.tech;Database=neondb;Username=user;Password=pass;SSL Mode=Require;"

# Keycloak
Keycloak__BaseUrl="http://keycloak:8080"                    # URL interna (se Keycloak estiver no mesmo cluster)
Keycloak__PublicBaseUrl="https://auth.seu-dominio.com"       # URL pública
Keycloak__Realm="assistenteexecutivo"
Keycloak__AdminUsername="admin"
Keycloak__AdminPassword="senha-segura"
Keycloak__ClientId="assistenteexecutivo-app"

# URLs
Api__PublicBaseUrl="https://assistente-api-xxx.run.app"
Frontend__PublicBaseUrl="https://assistente-web-xxx.run.app"
Frontend__CorsOrigins="https://assistente-web-xxx.run.app"

# Ambiente
ASPNETCORE_ENVIRONMENT="Production"
```

### Web - Variáveis Mínimas para Produção

```bash
NEXT_PUBLIC_API_BASE_URL="https://assistente-api-xxx.run.app"
```

---

## 🔐 Segurança

### Variáveis Sensíveis

As seguintes variáveis contêm informações sensíveis e devem ser protegidas:

- `ConnectionStrings__DefaultConnection` - Credenciais do banco
- `Keycloak__AdminPassword` - Senha do admin do Keycloak
- `Keycloak__ClientSecret` - Client secret do Keycloak
- `Keycloak__Google__ClientSecret` - Client secret do Google OAuth
- `Email__Smtp__Password` - Senha SMTP
- `Ocr__Azure__ApiKey` - Chave da API Azure
- `Ocr__Aws__SecretAccessKey` - Chave secreta AWS
- `Ocr__GoogleCloud__CredentialsJson` - Credenciais completas do Google

**Recomendação**: Use o Secret Manager do Google Cloud para essas variáveis.

---

## 🔄 Atualizar Variáveis Após Deploy

Se precisar atualizar variáveis sem fazer novo deploy:

```bash
# API
gcloud run services update assistente-api \
  --region us-central1 \
  --update-env-vars Keycloak__PublicBaseUrl="https://novo-dominio.com"

# Web
gcloud run services update assistente-web \
  --region us-central1 \
  --update-env-vars NEXT_PUBLIC_API_BASE_URL="https://nova-api.run.app"
```

---

## 📚 Referências

- [Google Cloud Run - Environment Variables](https://cloud.google.com/run/docs/configuring/environment-variables)
- [Google Secret Manager](https://cloud.google.com/secret-manager/docs)
- [ASP.NET Core Configuration](https://docs.microsoft.com/en-us/aspnet/core/fundamentals/configuration/)
- [Next.js Environment Variables](https://nextjs.org/docs/basic-features/environment-variables)

