# Integração com Cursor

Este guia mostra como usar o cliente API dentro do Cursor para automatizar tarefas.

## Configuração Inicial

1. Instale as dependências:
```bash
cd api-client
npm install
npm run build
```

2. Configure as variáveis de ambiente (opcional):
```bash
export API_BASE_URL="https://api.assistente.live"
export FRONTEND_URL="https://web.assistente.live"
```

## Uso no Cursor

### Exemplo 1: Script para listar contatos

Crie um arquivo `scripts/list-contacts.ts`:

```typescript
import AssistenteExecutivoClient from "../api-client/src/index";

const client = new AssistenteExecutivoClient();

async function listContacts() {
  const result = await client.contacts.list();
  console.log(JSON.stringify(result, null, 2));
}

listContacts();
```

Execute no Cursor:
```bash
npx ts-node scripts/list-contacts.ts
```

### Exemplo 2: Criar contato via Cursor

```typescript
import AssistenteExecutivoClient from "../api-client/src/index";

const client = new AssistenteExecutivoClient();

async function createContact(name: string, email: string) {
  const [firstName, ...lastNameParts] = name.split(" ");
  const lastName = lastNameParts.join(" ");

  const result = await client.contacts.create({
    firstName,
    lastName,
  });

  if (email) {
    await client.contacts.addEmail(result.contactId, email);
  }

  return result;
}

// Uso
createContact("João Silva", "joao@example.com").then(console.log);
```

### Exemplo 3: Criar lembrete

```typescript
import AssistenteExecutivoClient from "../api-client/src/index";

const client = new AssistenteExecutivoClient();

async function createReminder(contactId: string, reason: string, days: number) {
  const scheduledDate = new Date();
  scheduledDate.setDate(scheduledDate.getDate() + days);

  return await client.reminders.create({
    contactId,
    reason,
    scheduledFor: scheduledDate.toISOString(),
  });
}
```

### Exemplo 4: Consulta completa de dados

```typescript
import AssistenteExecutivoClient from "../api-client/src/index";

const client = new AssistenteExecutivoClient();

async function getFullContactInfo(contactId: string) {
  const contact = await client.contacts.getById(contactId);
  const notes = await client.notes.listByContact(contactId);
  const reminders = await client.reminders.list({ contactId });

  return {
    contact,
    notes,
    reminders: reminders.items,
  };
}
```

## Autenticação

Para usar o cliente, você precisa estar autenticado. O cliente usa cookies de sessão.

### Opção 1: Autenticação via navegador

1. Faça login no frontend (https://web.assistente.live)
2. Copie os cookies do navegador
3. Configure no cliente:

```typescript
const cookies = new Map<string, string>();
cookies.set("ae.sid", "seu-session-id");
cookies.set("csrf-token", "seu-csrf-token");

client.setCookies(cookies);
```

### Opção 2: Autenticação programática

```typescript
// Registrar novo usuário
await client.auth.register(
  "email@example.com",
  "senha123",
  "Nome",
  "Sobrenome"
);

// Depois fazer login via navegador ou OAuth
```

## Comandos Úteis

### Listar todos os contatos
```typescript
const allContacts = [];
let page = 1;
let hasMore = true;

while (hasMore) {
  const result = await client.contacts.list({ page, pageSize: 100 });
  allContacts.push(...result.items);
  hasMore = result.items.length === 100;
  page++;
}
```

### Buscar contatos por nome
```typescript
const results = await client.contacts.search("João");
```

### Listar lembretes pendentes
```typescript
const reminders = await client.reminders.list({
  status: ReminderStatus.Pending,
});
```

### Obter saldo de créditos
```typescript
const balance = await client.credits.getBalance();
console.log(`Saldo: ${balance.balance} créditos`);
```

## Troubleshooting

### Erro 401 (Não autorizado)
- Verifique se está autenticado: `await client.auth.isAuthenticated()`
- Configure os cookies corretamente

### Erro de conexão
- Verifique a URL da API: `client.getConfig().baseUrl`
- Verifique sua conexão com a internet

### Timeout
- Aumente o timeout: `new AssistenteExecutivoClient({ timeout: 60000 })`

