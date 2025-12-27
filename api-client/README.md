# Cliente API - Assistente Executivo

Cliente TypeScript/JavaScript para integração completa com a API do Assistente Executivo.

## Instalação

```bash
npm install
npm run build
```

## Uso Básico

```typescript
import AssistenteExecutivoClient from "@assistenteexecutivo/api-client";

// Criar instância do cliente
const client = new AssistenteExecutivoClient({
  baseUrl: "https://api.assistente.live",
  frontendUrl: "https://web.assistente.live"
});

// Verificar autenticação
const session = await client.auth.getSession();
if (!session.authenticated) {
  console.log("Usuário não autenticado. Faça login primeiro.");
}
```

## Funcionalidades

### Autenticação

```typescript
// Verificar sessão
const session = await client.auth.getSession();

// Verificar se está autenticado
const isAuth = await client.auth.isAuthenticated();

// Obter URL de login
const loginUrl = client.auth.getLoginUrl("/dashboard");

// Logout
await client.auth.logout();
```

### Contatos

```typescript
// Listar contatos
const contacts = await client.contacts.list({ page: 1, pageSize: 20 });

// Buscar contatos
const results = await client.contacts.search("João");

// Obter contato por ID
const contact = await client.contacts.getById("contact-id");

// Criar contato
const newContact = await client.contacts.create({
  firstName: "João",
  lastName: "Silva",
  email: "joao@example.com",
  company: "Empresa XYZ"
});

// Atualizar contato
await client.contacts.update("contact-id", {
  firstName: "João",
  lastName: "Silva Santos"
});

// Deletar contato
await client.contacts.delete("contact-id");

// Adicionar email
await client.contacts.addEmail("contact-id", "novo@example.com");

// Adicionar telefone
await client.contacts.addPhone("contact-id", "+5511999999999");

// Adicionar tag
await client.contacts.addTag("contact-id", "cliente-vip");
```

### Lembretes

```typescript
// Listar lembretes
const reminders = await client.reminders.list({
  status: ReminderStatus.Pending,
  page: 1,
  pageSize: 20
});

// Criar lembrete
const reminder = await client.reminders.create({
  contactId: "contact-id",
  reason: "Seguir sobre proposta",
  suggestedMessage: "Olá, gostaria de saber sobre a proposta...",
  scheduledFor: new Date("2024-12-31T10:00:00Z").toISOString()
});

// Atualizar status do lembrete
await client.reminders.updateStatus("reminder-id", {
  newStatus: ReminderStatus.Completed
});

// Deletar lembrete
await client.reminders.delete("reminder-id");
```

### Notas

```typescript
// Listar notas de um contato
const notes = await client.notes.listByContact("contact-id");

// Criar nota de texto
const note = await client.notes.createTextNote("contact-id", {
  text: "Reunião importante marcada para próxima semana"
});

// Atualizar nota
await client.notes.update("note-id", {
  rawContent: "Conteúdo atualizado"
});
```

### Automação (Drafts, Templates, Letterheads)

```typescript
// DRAFTS
const drafts = await client.automation.listDrafts();
const draft = await client.automation.createDraft({
  documentType: DocumentType.Letter,
  content: "Conteúdo do documento..."
});

// TEMPLATES
const templates = await client.automation.listTemplates();
const template = await client.automation.createTemplate({
  name: "Template de Email",
  type: TemplateType.Email,
  body: "Olá {{nome}}, ..."
});

// LETTERHEADS
const letterheads = await client.automation.listLetterheads();
const letterhead = await client.automation.createLetterhead({
  name: "Papel Timbrado Principal",
  designData: "{...}"
});
```

### Créditos

```typescript
// Obter saldo
const balance = await client.credits.getBalance();

// Listar transações
const transactions = await client.credits.getTransactions();

// Listar pacotes
const packages = await client.credits.listPackages();

// Comprar pacote
await client.credits.purchasePackage("package-id");
```

### Planos

```typescript
// Listar planos
const plans = await client.plans.list();
```

### Captura (OCR e Áudio)

```typescript
// Upload de cartão de visita
const result = await client.capture.uploadCard("./cartao.jpg");

// Processar nota de áudio
const audioResult = await client.capture.processAudioNote(
  "contact-id",
  "./audio.mp3"
);
```

### Configuração do Agente

```typescript
// Obter configuração
const config = await client.agentConfig.getCurrent();

// Atualizar configuração
await client.agentConfig.updateOrCreate({
  contextPrompt: "Você é um assistente executivo..."
});
```

## Integração com Cursor

Para usar no Cursor, você pode criar scripts que utilizam o cliente:

```typescript
// cursor-script.ts
import AssistenteExecutivoClient from "./api-client";

const client = new AssistenteExecutivoClient({
  baseUrl: process.env.API_URL || "https://api.assistente.live"
});

// Exemplo: Listar contatos
async function listContacts() {
  const result = await client.contacts.list();
  console.log(`Total de contatos: ${result.totalCount}`);
  result.items.forEach(contact => {
    console.log(`- ${contact.firstName} ${contact.lastName}`);
  });
}

listContacts();
```

## Configuração

O cliente aceita as seguintes opções de configuração:

```typescript
interface ApiClientConfig {
  baseUrl: string;              // URL base da API
  frontendUrl?: string;          // URL do frontend (para redirects)
  timeout?: number;              // Timeout das requisições (ms)
}
```

Variáveis de ambiente suportadas:
- `API_BASE_URL`: URL base da API
- `FRONTEND_URL`: URL do frontend

## Autenticação

O cliente utiliza cookies de sessão para autenticação. Para usar com autenticação existente:

```typescript
// Definir cookies manualmente
const cookies = new Map<string, string>();
cookies.set("ae.sid", "session-id-value");
cookies.set("csrf-token", "csrf-token-value");

client.setCookies(cookies);
```

## Exemplos

Veja a pasta `examples/` para mais exemplos de uso.

