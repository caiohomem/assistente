# Resumo do Cliente API - Assistente Executivo

## ✅ O que foi criado

Um cliente TypeScript/JavaScript completo para integração com a API do Assistente Executivo, com acesso a **todas as funcionalidades** da API.

## 📦 Estrutura do Projeto

```
api-client/
├── src/
│   ├── index.ts              # Cliente principal
│   ├── types.ts              # Tipos TypeScript
│   ├── config.ts             # Configuração
│   ├── http-client.ts        # Cliente HTTP base
│   ├── auth.ts               # Autenticação
│   ├── contacts.ts           # CRUD de contatos
│   ├── reminders.ts          # CRUD de lembretes
│   ├── notes.ts              # CRUD de notas
│   ├── automation.ts         # Drafts, Templates, Letterheads
│   ├── credits.ts            # Créditos e transações
│   ├── plans.ts              # Planos
│   ├── capture.ts            # OCR e processamento de áudio
│   └── agent-config.ts       # Configuração do agente
├── examples/                 # Exemplos de uso
├── scripts/                  # Scripts utilitários
├── README.md                 # Documentação principal
├── QUICK_START.md            # Guia rápido
├── EXAMPLES.md               # Exemplos detalhados
└── cursor-integration.md     # Guia de integração com Cursor
```

## 🎯 Funcionalidades Implementadas

### ✅ Autenticação
- Verificar sessão
- Login/Logout
- Registro de usuário
- Gerenciamento de cookies

### ✅ Contatos (CRUD Completo)
- Listar contatos (com paginação)
- Buscar contatos
- Obter contato por ID
- Criar contato
- Atualizar contato
- Deletar contato
- Adicionar email
- Adicionar telefone
- Adicionar tag
- Adicionar relacionamento
- Deletar relacionamento

### ✅ Lembretes (CRUD Completo)
- Listar lembretes (com filtros)
- Obter lembrete por ID
- Criar lembrete
- Atualizar status do lembrete
- Deletar lembrete

### ✅ Notas
- Listar notas de um contato
- Obter nota por ID
- Criar nota de texto
- Atualizar nota
- Deletar nota
- Obter arquivo de áudio
- Obter arquivo de mídia

### ✅ Automação
- **Drafts**: CRUD completo (criar, listar, atualizar, aprovar, enviar, deletar)
- **Templates**: CRUD completo
- **Letterheads**: CRUD completo

### ✅ Créditos
- Obter saldo
- Listar transações
- Listar pacotes
- Comprar pacote

### ✅ Planos
- Listar planos disponíveis

### ✅ Captura
- Upload de cartão de visita (OCR)
- Processamento de nota de áudio
- Listar jobs de captura
- Obter job por ID

### ✅ Configuração do Agente
- Obter configuração atual
- Atualizar configuração

## 🚀 Como Usar

### Instalação

```bash
cd api-client
npm install
npm run build
```

### Uso Básico

```typescript
import AssistenteExecutivoClient from "./src/index";

const client = new AssistenteExecutivoClient();

// Verificar autenticação
const session = await client.auth.getSession();

// Listar contatos
const contacts = await client.contacts.list();

// Criar contato
const contact = await client.contacts.create({
  firstName: "João",
  lastName: "Silva"
});

// Criar lembrete
const reminder = await client.reminders.create({
  contactId: contact.contactId,
  reason: "Seguir proposta",
  scheduledFor: new Date().toISOString()
});
```

### Scripts de Linha de Comando

```bash
# Listar contatos
npx ts-node scripts/cursor-helper.ts list-contacts

# Criar contato
npx ts-node scripts/cursor-helper.ts create-contact "João Silva" "joao@example.com"

# Criar lembrete
npx ts-node scripts/cursor-helper.ts create-reminder <id> "Seguir proposta" 7

# Listar lembretes
npx ts-node scripts/cursor-helper.ts list-reminders

# Ver saldo
npx ts-node scripts/cursor-helper.ts get-balance
```

## 📚 Documentação

- **README.md**: Documentação completa da API
- **QUICK_START.md**: Guia rápido de início
- **EXAMPLES.md**: Exemplos detalhados de uso
- **cursor-integration.md**: Guia de integração com Cursor

## 🔧 Configuração

O cliente aceita configuração via construtor ou variáveis de ambiente:

```typescript
const client = new AssistenteExecutivoClient({
  baseUrl: "https://api.assistente.live",
  frontendUrl: "https://web.assistente.live",
  timeout: 30000
});
```

Variáveis de ambiente:
- `API_BASE_URL`
- `FRONTEND_URL`

## 🔐 Autenticação

O cliente usa cookies de sessão. Para usar:

1. Faça login no frontend (https://web.assistente.live)
2. Copie os cookies do navegador
3. Configure no cliente:

```typescript
const cookies = new Map<string, string>();
cookies.set("ae.sid", "session-id");
client.setCookies(cookies);
```

## ✨ Características

- ✅ **TypeScript completo** com tipos para todas as APIs
- ✅ **Acesso a todas as APIs** disponíveis no backend
- ✅ **Fácil de usar** com API intuitiva
- ✅ **Pronto para Cursor** com scripts e exemplos
- ✅ **Bem documentado** com exemplos e guias
- ✅ **Suporte a uploads** (FormData para arquivos)
- ✅ **Gerenciamento de cookies** automático
- ✅ **Tratamento de erros** robusto

## 📝 Próximos Passos

1. Instale as dependências: `npm install`
2. Compile o projeto: `npm run build`
3. Teste os exemplos: `npm run example:basic`
4. Use no Cursor: `npx ts-node scripts/cursor-helper.ts list-contacts`

## 🎉 Pronto para Usar!

O cliente está completo e pronto para ser usado. Todas as APIs estão implementadas e documentadas.

