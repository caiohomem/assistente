/**
 * Script helper para uso no Cursor
 * 
 * Este script fornece funções úteis para interagir com a API do Assistente Executivo
 * diretamente do Cursor.
 * 
 * Uso:
 *   npx ts-node scripts/cursor-helper.ts <comando> [argumentos]
 * 
 * Comandos disponíveis:
 *   - list-contacts: Lista todos os contatos
 *   - create-contact <nome> <email>: Cria um novo contato
 *   - create-reminder <contactId> <reason> <days>: Cria um lembrete
 *   - list-reminders: Lista todos os lembretes
 *   - get-balance: Mostra saldo de créditos
 */

import AssistenteExecutivoClient from "../src/index";
import { ReminderStatus } from "../src/types";

const client = new AssistenteExecutivoClient({
  baseUrl: process.env.API_BASE_URL || "https://api.assistente.live",
  frontendUrl: process.env.FRONTEND_URL || "https://web.assistente.live",
});

async function checkAuth() {
  const isAuth = await client.auth.isAuthenticated();
  if (!isAuth) {
    console.error("❌ Você precisa estar autenticado!");
    console.log(`🔗 Faça login em: ${client.auth.getLoginUrl()}`);
    process.exit(1);
  }
}

async function listContacts() {
  await checkAuth();
  const result = await client.contacts.list({ page: 1, pageSize: 100 });
  console.log(`\n📇 Total de contatos: ${result.totalCount}\n`);
  result.items.forEach((contact) => {
    console.log(`  ${contact.contactId}`);
    console.log(`  Nome: ${contact.firstName} ${contact.lastName || ""}`);
    if (contact.company) console.log(`  Empresa: ${contact.company}`);
    if (contact.emails.length > 0) {
      console.log(`  Emails: ${contact.emails.map((e) => e.email).join(", ")}`);
    }
    if (contact.phones.length > 0) {
      console.log(`  Telefones: ${contact.phones.map((p) => p.phone).join(", ")}`);
    }
    console.log("");
  });
}

async function createContact(name: string, email?: string) {
  await checkAuth();
  const [firstName, ...lastNameParts] = name.split(" ");
  const lastName = lastNameParts.join(" ") || undefined;

  console.log(`\n📇 Criando contato: ${firstName} ${lastName || ""}...`);
  const result = await client.contacts.create({
    firstName,
    lastName,
  });

  console.log(`✅ Contato criado: ${result.contactId}`);

  if (email) {
    await client.contacts.addEmail(result.contactId, email);
    console.log(`✅ Email adicionado: ${email}`);
  }

  return result;
}

async function createReminder(contactId: string, reason: string, days: number) {
  await checkAuth();
  const scheduledDate = new Date();
  scheduledDate.setDate(scheduledDate.getDate() + days);

  console.log(`\n⏰ Criando lembrete para ${days} dias...`);
  const result = await client.reminders.create({
    contactId,
    reason,
    scheduledFor: scheduledDate.toISOString(),
  });

  console.log(`✅ Lembrete criado: ${result.reminderId}`);
  console.log(`   Agendado para: ${scheduledDate.toLocaleString("pt-BR")}`);

  return result;
}

async function listReminders(status?: string) {
  await checkAuth();
  const reminderStatus = status
    ? (ReminderStatus as any)[status]
    : undefined;

  const result = await client.reminders.list({
    status: reminderStatus,
    page: 1,
    pageSize: 100,
  });

  console.log(`\n⏰ Total de lembretes: ${result.totalCount}\n`);
  result.items.forEach((reminder) => {
    console.log(`  ${reminder.reminderId}`);
    console.log(`  Motivo: ${reminder.reason}`);
    console.log(`  Status: ${reminder.status}`);
    console.log(`  Agendado para: ${new Date(reminder.scheduledFor).toLocaleString("pt-BR")}`);
    if (reminder.suggestedMessage) {
      console.log(`  Mensagem sugerida: ${reminder.suggestedMessage.substring(0, 50)}...`);
    }
    console.log("");
  });
}

async function getBalance() {
  await checkAuth();
  const balance = await client.credits.getBalance();
  console.log(`\n💰 Saldo de créditos: ${balance.balance}`);
  console.log(`   Total de transações: ${balance.transactionCount}`);
}

async function searchContacts(term: string) {
  await checkAuth();
  const result = await client.contacts.search(term);
  console.log(`\n🔍 Resultados da busca por "${term}": ${result.totalCount}\n`);
  result.items.forEach((contact) => {
    console.log(`  ${contact.firstName} ${contact.lastName || ""} (${contact.contactId})`);
  });
}

// Main
async function main() {
  const command = process.argv[2];
  const args = process.argv.slice(3);

  try {
    switch (command) {
      case "list-contacts":
        await listContacts();
        break;

      case "create-contact":
        if (args.length < 1) {
          console.error("Uso: create-contact <nome> [email]");
          process.exit(1);
        }
        await createContact(args[0], args[1]);
        break;

      case "create-reminder":
        if (args.length < 3) {
          console.error("Uso: create-reminder <contactId> <reason> <days>");
          process.exit(1);
        }
        await createReminder(args[0], args[1], parseInt(args[2]));
        break;

      case "list-reminders":
        await listReminders(args[0]);
        break;

      case "get-balance":
        await getBalance();
        break;

      case "search":
        if (args.length < 1) {
          console.error("Uso: search <termo>");
          process.exit(1);
        }
        await searchContacts(args[0]);
        break;

      default:
        console.log(`
Uso: npx ts-node scripts/cursor-helper.ts <comando> [argumentos]

Comandos disponíveis:
  list-contacts                    Lista todos os contatos
  create-contact <nome> [email]    Cria um novo contato
  create-reminder <id> <reason> <days>  Cria um lembrete
  list-reminders [status]          Lista lembretes (opcional: Pending, Completed, Cancelled, Snoozed)
  get-balance                      Mostra saldo de créditos
  search <termo>                   Busca contatos por termo

Exemplos:
  npx ts-node scripts/cursor-helper.ts list-contacts
  npx ts-node scripts/cursor-helper.ts create-contact "João Silva" "joao@example.com"
  npx ts-node scripts/cursor-helper.ts create-reminder <contact-id> "Seguir proposta" 7
  npx ts-node scripts/cursor-helper.ts list-reminders Pending
  npx ts-node scripts/cursor-helper.ts search "João"
        `);
        process.exit(1);
    }
  } catch (error: any) {
    console.error("❌ Erro:", error.message);
    if (error.statusCode) {
      console.error(`   Status: ${error.statusCode}`);
    }
    process.exit(1);
  }
}

main();

