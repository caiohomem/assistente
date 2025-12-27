# Variáveis de Ambiente - Referência Rápida

## 🌐 Web (Frontend)

| Variável | Obrigatória | Descrição |
|----------|-------------|-----------|
| `NEXT_PUBLIC_API_BASE_URL` | ✅ Sim | URL pública da API (ex: `https://assistente-api-xxx.run.app`) |

---

## 🔧 API (Backend)

### Obrigatórias

| Variável | Descrição | Exemplo |
|----------|-----------|---------|
| `ConnectionStrings__DefaultConnection` | Connection string do banco | `Host=...;Database=...;Username=...;Password=...;SSL Mode=Require;` |
| `ConnectionStrings__Redis` | Connection string do Redis (formato StackExchange.Redis) | `host:6379,password=...,ssl=true,abortConnect=false` |
| `Keycloak__BaseUrl` | URL interna do Keycloak | `http://keycloak:8080` |
| `Keycloak__PublicBaseUrl` | URL pública do Keycloak (HTTPS) | `https://auth.seu-dominio.com` |
| `Keycloak__Realm` | Nome do realm | `assistenteexecutivo` |
| `Keycloak__AdminUsername` | Username do admin | `admin` |
| `Keycloak__AdminPassword` | Password do admin | `senha-segura` |
| `Keycloak__ClientId` | Client ID da aplicação | `assistenteexecutivo-app` |
| `Api__PublicBaseUrl` | URL pública da API (⚠️ obrigatória para OAuth) | `https://assistente-api-xxx.run.app` |
| `Frontend__PublicBaseUrl` | URL pública do frontend | `https://assistente-web-xxx.run.app` |
| `Frontend__CorsOrigins` | Origens permitidas (CORS) | `https://assistente-web-xxx.run.app` |
| `ASPNETCORE_ENVIRONMENT` | Ambiente da aplicação | `Production` |

### Opcionais

#### Redis (Session Storage)
- `ConnectionStrings__Redis` (formato StackExchange.Redis: `host:port,password=...,ssl=true,abortConnect=false,connectTimeout=15000`) - **Recomendado**
- `Redis__ConnectionString` ou `Redis__Configuration`

**Nota**: Se Redis não estiver configurado:
- PostgreSQL: usa Memory Cache (sessões perdidas ao reiniciar)
- SQL Server: usa SQL Server Cache (sessões persistidas no banco)

### Opcionais (Configurações Avançadas)

#### Email
- `Email__Smtp__Host`, `Email__Smtp__Port`, `Email__Smtp__User`, `Email__Smtp__Password`, `Email__Smtp__From`, `Email__Smtp__FromName`, `Email__Smtp__EnableSsl`

#### OCR
- `Ocr__Provider` (Stub, PaddleOcr, Azure, GoogleCloud, Aws)
- `Ocr__PaddleOcr__BaseUrl`, `Ocr__PaddleOcr__Lang`
- `Ocr__Azure__Endpoint`, `Ocr__Azure__ApiKey`, `Ocr__Azure__ApiVersion`
- `Ocr__GoogleCloud__ProjectId`, `Ocr__GoogleCloud__CredentialsJson`
- `Ocr__Aws__Region`, `Ocr__Aws__AccessKeyId`, `Ocr__Aws__SecretAccessKey`

#### Ollama (LLM)
- `Ollama__BaseUrl`
- `Ollama__Ocr__Model`, `Ollama__Ocr__Temperature`, `Ollama__Ocr__MaxTokens`
- `Ollama__LLM__Provider`, `Ollama__LLM__Model`, `Ollama__LLM__Temperature`, `Ollama__LLM__MaxTokens`

#### Whisper (Áudio)
- `Whisper__Provider`, `Whisper__Model`, `Whisper__Language`, `Whisper__ApiUrl`

#### Keycloak (Opcionais)
- `Keycloak__RealmName`, `Keycloak__AdminRealm`, `Keycloak__AdminClientId`, `Keycloak__AdminClientSecret`, `Keycloak__ClientSecret`, `Keycloak__ThemeName`
- `Keycloak__GoogleRedirectUri`
- `Keycloak__Google__ClientId`, `Keycloak__Google__ClientSecret`

---

## 📋 Formato no Cloud Run

No Cloud Run, use `__` (dois underscores) para separar níveis de configuração:

```bash
# Correto
ConnectionStrings__DefaultConnection="..."
Keycloak__PublicBaseUrl="..."
Email__Smtp__Host="..."

# Errado (não funciona)
ConnectionStrings:DefaultConnection="..."
Keycloak.PublicBaseUrl="..."
```

---

## 🔐 Variáveis Sensíveis (Use Secret Manager)

- `ConnectionStrings__DefaultConnection`
- `Keycloak__AdminPassword`
- `Keycloak__ClientSecret`
- `Keycloak__Google__ClientSecret`
- `Email__Smtp__Password`
- `Ocr__Azure__ApiKey`
- `Ocr__Aws__SecretAccessKey`
- `Ocr__GoogleCloud__CredentialsJson`

---

## 📚 Documentação Completa

Para detalhes completos, consulte [ENV_VARIABLES.md](./ENV_VARIABLES.md)

