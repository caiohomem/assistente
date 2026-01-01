# n8n System Workflows

Este diretório contém os dois workflows sistêmicos que formam a base do sistema de automação.

## Arquitetura

```
┌─────────────────┐     ┌──────────────────┐     ┌─────────────────┐
│  Backend API    │────▶│  Flow Builder    │────▶│  n8n Workflow   │
│  (Spec JSON)    │     │  (compila spec)  │     │  (criado)       │
└─────────────────┘     └──────────────────┘     └─────────────────┘
                                                          │
┌─────────────────┐     ┌──────────────────┐              ▼
│  Backend API    │────▶│  Flow Runner     │────▶│  Execução       │
│  (inputs)       │     │  (executa)       │     │  determinística │
└─────────────────┘     └──────────────────┘     └─────────────────┘
```

## Workflows

### 1. Flow Builder (`flow-builder.json`)

**Endpoint:** `POST /webhook/system/flows/build`

**Propósito:** Transforma uma Spec JSON em um workflow n8n real.

**Input:**
```json
{
  "spec": {
    "name": "Meu Workflow",
    "trigger": { "type": "Manual" },
    "steps": [...]
  },
  "tenantId": "default",
  "requestedBy": "user-id",
  "idempotencyKey": "unique-key"
}
```

**Output:**
```json
{
  "success": true,
  "workflowId": "n8n-workflow-id",
  "specId": "spec-uuid",
  "specVersion": 1,
  "warnings": [],
  "compiledAt": "2024-01-01T00:00:00Z"
}
```

### 2. Flow Runner (`flow-runner.json`)

**Endpoint:** `POST /webhook/system/flows/run`

**Propósito:** Executa um workflow existente com inputs específicos.

**Input:**
```json
{
  "workflowId": "n8n-workflow-id",
  "inputs": { "param1": "value1" },
  "tenantId": "default",
  "requestedBy": "user-id",
  "waitForCompletion": true,
  "timeoutSeconds": 300
}
```

**Output (síncrono):**
```json
{
  "success": true,
  "runId": "run-uuid",
  "executionId": "n8n-execution-id",
  "status": "success",
  "result": {...},
  "startedAt": "2024-01-01T00:00:00Z",
  "finishedAt": "2024-01-01T00:00:05Z"
}
```

**Output (assíncrono, `waitForCompletion: false`):**
```json
{
  "success": true,
  "async": true,
  "runId": "run-uuid",
  "executionId": "n8n-execution-id",
  "status": "Accepted",
  "message": "Poll /api/workflows/runs/{runId} for status."
}
```

## Instalação

### 1. Configurar Variáveis de Ambiente no n8n

```bash
# API do Backend
API_BASE_URL=https://api.assistente.live

# API do n8n
N8N_API_URL=https://n8n.assistente.live
N8N_API_KEY=your-n8n-api-key

# Keycloak OAuth2 (client_credentials)
KEYCLOAK_TOKEN_URL=https://keycloak.callback-local-cchagas.xyz/realms/assistenteexecutivo/protocol/openid-connect/token
KEYCLOAK_CLIENT_ID=assistente-api
KEYCLOAK_CLIENT_SECRET=zKyN2pgv0Qx4LmrwkHYKITjOhoxmOhWd

# Opcional
WHATSAPP_API_URL=https://api.whatsapp.com
```

### 2. Criar Credentials no n8n

Apenas uma credential é necessária agora:

1. **n8n API Auth** (Header Auth):
   - Name: `n8n-api-auth`
   - Header Name: `X-N8N-API-KEY`
   - Header Value: `{sua-api-key-n8n}`

> **Nota:** A autenticação com o backend agora usa OAuth2 client_credentials via Keycloak. O token é obtido automaticamente em cada execução.

### 3. Importar Workflows

Via UI do n8n:
1. Vá em **Workflows** → **Import from File**
2. Selecione `flow-builder.json`
3. Repita para `flow-runner.json`
4. Ative ambos os workflows

Via API:
```bash
# Flow Builder
curl -X POST "https://n8n.assistente.live/api/v1/workflows" \
  -H "X-N8N-API-KEY: your-key" \
  -H "Content-Type: application/json" \
  -d @flow-builder.json

# Flow Runner
curl -X POST "https://n8n.assistente.live/api/v1/workflows" \
  -H "X-N8N-API-KEY: your-key" \
  -H "Content-Type: application/json" \
  -d @flow-runner.json
```

### 4. Ativar Workflows

```bash
# Ativar Flow Builder
curl -X PATCH "https://n8n.assistente.live/api/v1/workflows/{flow-builder-id}" \
  -H "X-N8N-API-KEY: your-key" \
  -H "Content-Type: application/json" \
  -d '{"active": true}'

# Ativar Flow Runner
curl -X PATCH "https://n8n.assistente.live/api/v1/workflows/{flow-runner-id}" \
  -H "X-N8N-API-KEY: your-key" \
  -H "Content-Type: application/json" \
  -d '{"active": true}'
```

## Endpoints da API Backend Necessários

O backend precisa expor estes endpoints para os workflows funcionarem:

### Para o Flow Builder:
- `POST /api/workflows/specs` - Salvar spec
- `PUT /api/workflows/specs/{specId}/bind` - Vincular spec ao workflow n8n

### Para o Flow Runner:
- `GET /api/workflows/runs/check?idempotencyKey={key}` - Verificar idempotência
- `GET /api/workflows/specs/{specId}/resolve` - Resolver spec para workflow ID
- `POST /api/workflows/runs` - Registrar execução
- `PUT /api/workflows/runs/{runId}` - Atualizar status da execução

## Segurança

1. **Autenticação:** Ambos workflows usam Header Auth
2. **Allowlist:** O Flow Builder valida URLs contra hosts permitidos
3. **Idempotência:** Ambos suportam idempotencyKey para evitar duplicação
4. **Isolamento:** Workflows são taggeados com `tenant:{id}`
5. **Secrets:** Nunca incluídos no spec, sempre via n8n Credentials
