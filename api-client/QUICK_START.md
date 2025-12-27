# Quick Start - Cliente API Assistente Executivo

## Instalação Rápida

```bash
cd api-client
npm install
npm run build
```

## Configuração

Configure as variáveis de ambiente (opcional):

```bash
export API_BASE_URL="https://api.assistente.live"
export FRONTEND_URL="https://web.assistente.live"
```

Ou use os valores padrão que já estão configurados.

## Uso Básico

### 1. Verificar Autenticação

```typescript
import AssistenteExecutivoClient from "./src/index";

const client = new AssistenteExecutivoClient();
const session = await client.auth.getSession();

if (session.authenticated) {
  console.log("Autenticado como:", session.user?.email);
} else {
  console.log("Não autenticado. Faça login em:", client.auth.getLoginUrl());
}
```

### 2. Listar Contatos

```typescript
const contacts = await client.contacts.list({ page: 1, pageSize: 20 });
console.log(`Total: ${contacts.totalCount}`);
contacts.items.forEach(c => console.log(`${c.firstName} ${c.lastName}`));
```

### 3. Criar Contato

```typescript
const result = await client.contacts.create({
  firstName: "João",
  lastName: "Silva",
  company: "Empresa XYZ"
});
console.log("Contato criado:", result.contactId);
```

### 4. Criar Lembrete

```typescript
const scheduledDate = new Date();
scheduledDate.setDate(scheduledDate.getDate() + 7); // 7 dias

const reminder = await client.reminders.create({
  contactId: "contact-id",
  reason: "Seguir sobre proposta",
  scheduledFor: scheduledDate.toISOString()
});
```

### 5. Listar Lembretes

```typescript
const reminders = await client.reminders.list({
  status: ReminderStatus.Pending
});
```

## Scripts de Linha de Comando

Use o script helper para comandos rápidos:

```bash
# Listar contatos
npx ts-node scripts/cursor-helper.ts list-contacts

# Criar contato
npx ts-node scripts/cursor-helper.ts create-contact "João Silva" "joao@example.com"

# Criar lembrete
npx ts-node scripts/cursor-helper.ts create-reminder <contact-id> "Seguir proposta" 7

# Listar lembretes pendentes
npx ts-node scripts/cursor-helper.ts list-reminders Pending

# Ver saldo de créditos
npx ts-node scripts/cursor-helper.ts get-balance

# Buscar contatos
npx ts-node scripts/cursor-helper.ts search "João"
```

## Autenticação

O cliente usa cookies de sessão. Para usar:

1. **Opção 1**: Faça login no navegador (https://web.assistente.live) e copie os cookies
2. **Opção 2**: Use autenticação OAuth via navegador

Para configurar cookies manualmente:

```typescript
const cookies = new Map<string, string>();
cookies.set("ae.sid", "seu-session-id");
client.setCookies(cookies);
```

## Exemplos Completos

Veja os exemplos na pasta `examples/`:

- `basic-usage.ts`: Exemplo básico de uso
- `create-contact-and-reminder.ts`: Criar contato e lembrete

Execute:

```bash
npm run build
node dist/examples/basic-usage.js
```

## Próximos Passos

- Leia o [README.md](./README.md) para documentação completa
- Veja [cursor-integration.md](./cursor-integration.md) para integração com Cursor
- Explore os exemplos na pasta `examples/`

