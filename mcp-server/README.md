# Assistente Executivo MCP Server

Servidor MCP (Model Context Protocol) para integração com a API do Assistente Executivo. Este servidor permite que ferramentas como o Cursor acessem e manipulem dados da API através de ferramentas MCP.

## 🚀 Quick Start

**Quer começar rapidamente?** Veja o [QUICK_START.md](./QUICK_START.md) para um guia de 5 minutos.

**Quer um guia completo?** Veja o [GUIA_USO.md](./GUIA_USO.md) para instruções detalhadas.

## Funcionalidades

O servidor MCP fornece acesso completo a todas as APIs do Assistente Executivo:

### Contatos
- ✅ Listar contatos
- ✅ Buscar contatos
- ✅ Obter contato por ID
- ✅ Criar contato
- ✅ Atualizar contato
- ✅ Deletar contato
- ✅ Adicionar email ao contato
- ✅ Adicionar telefone ao contato
- ✅ Adicionar tag ao contato
- ✅ Adicionar relacionamento entre contatos
- ✅ Deletar relacionamento

### Lembretes
- ✅ Criar lembrete
- ✅ Listar lembretes
- ✅ Obter lembrete por ID
- ✅ Atualizar status do lembrete
- ✅ Deletar lembrete

### Notas
- ✅ Listar notas de um contato
- ✅ Obter nota por ID
- ✅ Criar nota de texto
- ✅ Atualizar nota
- ✅ Deletar nota

### Automação (Drafts, Templates, Letterheads)
- ✅ CRUD completo para Drafts
- ✅ CRUD completo para Templates
- ✅ CRUD completo para Letterheads
- ✅ Aprovar e enviar drafts

### Créditos
- ✅ Obter saldo de créditos
- ✅ Listar transações de crédito
- ✅ Listar pacotes de créditos
- ✅ Comprar pacote de créditos

### Configuração do Agente
- ✅ Obter configuração do agente
- ✅ Atualizar configuração do agente

### Captura
- ✅ Obter job de captura por ID
- ✅ Listar jobs de captura

### Planos
- ✅ Listar planos disponíveis

## Instalação

1. Instale as dependências:

```bash
cd mcp-server
npm install
```

2. Compile o projeto:

```bash
npm run build
```

## Configuração

O servidor MCP usa variáveis de ambiente para configuração:

- `API_BASE_URL`: URL base da API (padrão: `http://localhost:5239`)
- `ACCESS_TOKEN`: Token JWT de autenticação (obrigatório para operações autenticadas)

### Como obter o token de acesso

**Você NÃO precisa criar um novo client no Keycloak!** O client `assistenteexecutivo-app` já está configurado e pode ser usado.

#### Método 1: Usar Script (Recomendado) ✅

**Windows (PowerShell):**
```powershell
cd mcp-server
.\scripts\get-token.ps1 -Email "seu_email@exemplo.com" -Password "sua_senha" -Save
```

**Linux/macOS (Node.js):**
```bash
cd mcp-server
node scripts/get-token.js seu_email@exemplo.com sua_senha --save
```

O script irá:
- Obter o token do Keycloak
- Mostrar o token e informações
- Salvar no arquivo `.env.local` (se usar `--save` ou `-Save`)

#### Método 2: Usar curl

```bash
curl -X POST "https://keycloak.callback-local-cchagas.xyz/realms/assistenteexecutivo/protocol/openid-connect/token" \
  -H "Content-Type: application/x-www-form-urlencoded" \
  -d "grant_type=password" \
  -d "client_id=assistenteexecutivo-app" \
  -d "username=seu_email@exemplo.com" \
  -d "password=sua_senha"
```

Extraia o `access_token` da resposta JSON.

#### Método 3: Via Browser (se já estiver autenticado)

Se você já está autenticado na aplicação web, pode obter o token da sessão através do endpoint `/auth/session`.

**Para mais detalhes, veja o arquivo [OBTER_TOKEN.md](./OBTER_TOKEN.md)**

### Configurar variáveis de ambiente

```bash
export API_BASE_URL=http://localhost:5239
export ACCESS_TOKEN=seu_token_jwt_aqui
```

Ou use o arquivo `.env.local` (criado automaticamente pelo script com `--save`):
```bash
ACCESS_TOKEN=seu_token_jwt_aqui
API_BASE_URL=http://localhost:5239
```

## Uso com Cursor

Para usar este servidor MCP com o Cursor, adicione a seguinte configuração no arquivo de configuração do MCP do Cursor:

### Windows
Adicione em `%APPDATA%\Cursor\User\globalStorage\saoudrizwan.claude-dev\settings\cline_mcp_settings.json`:

```json
{
  "mcpServers": {
    "assistente-executivo": {
      "command": "node",
      "args": [
        "C:\\caminho\\para\\AssistenteExecutivo\\mcp-server\\dist\\index.js"
      ],
      "env": {
        "API_BASE_URL": "http://localhost:5239",
        "ACCESS_TOKEN": "seu_token_jwt_aqui"
      }
    }
  }
}
```

### macOS/Linux
Adicione em `~/.config/Cursor/User/globalStorage/saoudrizwan.claude-dev/settings/cline_mcp_settings.json`:

```json
{
  "mcpServers": {
    "assistente-executivo": {
      "command": "node",
      "args": [
        "/caminho/para/AssistenteExecutivo/mcp-server/dist/index.js"
      ],
      "env": {
        "API_BASE_URL": "http://localhost:5239",
        "ACCESS_TOKEN": "seu_token_jwt_aqui"
      }
    }
  }
}
```

**Nota**: Ajuste o caminho absoluto para o arquivo `dist/index.js` conforme sua instalação.

## Desenvolvimento

Para desenvolvimento com hot-reload:

```bash
npm run dev
```

## Exemplos de Uso

### Criar um contato

```typescript
// O Cursor pode usar a ferramenta create_contact
{
  "name": "create_contact",
  "arguments": {
    "firstName": "João",
    "lastName": "Silva",
    "company": "Empresa XYZ",
    "jobTitle": "Gerente"
  }
}
```

### Criar um lembrete

```typescript
{
  "name": "create_reminder",
  "arguments": {
    "contactId": "guid-do-contato",
    "reason": "Follow-up sobre proposta",
    "scheduledFor": "2024-12-25T10:00:00Z"
  }
}
```

### Listar contatos

```typescript
{
  "name": "list_contacts",
  "arguments": {
    "page": 1,
    "pageSize": 20
  }
}
```

## Estrutura do Projeto

```
mcp-server/
├── src/
│   ├── index.ts              # Servidor MCP principal
│   ├── api-client.ts         # Cliente HTTP para a API
│   └── tools/
│       ├── contacts.ts       # Ferramentas de contatos
│       ├── reminders.ts      # Ferramentas de lembretes
│       ├── notes.ts          # Ferramentas de notas
│       ├── automation.ts     # Ferramentas de automação
│       ├── credits.ts        # Ferramentas de créditos
│       ├── agent-config.ts   # Ferramentas de configuração
│       ├── capture.ts        # Ferramentas de captura
│       └── plans.ts          # Ferramentas de planos
├── package.json
├── tsconfig.json
└── README.md
```

## Autenticação

A maioria das operações requer autenticação via token JWT. O token deve ser fornecido através da variável de ambiente `ACCESS_TOKEN` ou configurado dinamicamente no cliente da API.

Algumas operações (como `list_plans`) são públicas e não requerem autenticação.

## Troubleshooting

### Erro de autenticação
- Verifique se o `ACCESS_TOKEN` está configurado corretamente
- Certifique-se de que o token não expirou
- Verifique se a URL da API está correta

### Erro de conexão
- Verifique se a API está rodando
- Verifique se a `API_BASE_URL` está correta
- Verifique se há problemas de CORS (não aplicável para MCP, mas pode afetar testes diretos)

### Ferramenta não encontrada
- Certifique-se de que o servidor foi compilado (`npm run build`)
- Verifique se o nome da ferramenta está correto
- Verifique os logs do servidor para mais detalhes

## Licença

MIT

