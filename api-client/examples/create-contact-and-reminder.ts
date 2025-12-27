/**
 * Exemplo: Criar um contato e um lembrete associado
 */

import AssistenteExecutivoClient from "../src/index";
import { ReminderStatus } from "../src/types";

async function main() {
  const client = new AssistenteExecutivoClient({
    baseUrl: process.env.API_BASE_URL || "https://api.assistente.live",
  });

  try {
    // Verificar autenticação
    const isAuth = await client.auth.isAuthenticated();
    if (!isAuth) {
      console.log("❌ Faça login primeiro!");
      return;
    }

    console.log("✅ Autenticado");

    // Criar um novo contato
    console.log("\n📇 Criando contato...");
    const contactResult = await client.contacts.create({
      firstName: "Maria",
      lastName: "Santos",
      jobTitle: "Diretora de Vendas",
      company: "Empresa ABC",
      email: "maria@empresaabc.com",
    });

    const contactId = contactResult.contactId;
    console.log(`✅ Contato criado: ${contactId}`);

    // Adicionar telefone ao contato
    console.log("\n📞 Adicionando telefone...");
    await client.contacts.addPhone(contactId, "+5511999999999");
    console.log("✅ Telefone adicionado");

    // Adicionar email ao contato
    console.log("\n📧 Adicionando email...");
    await client.contacts.addEmail(contactId, "maria.santos@empresaabc.com");
    console.log("✅ Email adicionado");

    // Criar um lembrete para o contato
    console.log("\n⏰ Criando lembrete...");
    const scheduledDate = new Date();
    scheduledDate.setDate(scheduledDate.getDate() + 7); // 7 dias a partir de hoje

    const reminderResult = await client.reminders.create({
      contactId: contactId,
      reason: "Seguir sobre proposta comercial",
      suggestedMessage: "Olá Maria, gostaria de saber se teve oportunidade de revisar nossa proposta...",
      scheduledFor: scheduledDate.toISOString(),
    });

    console.log(`✅ Lembrete criado: ${reminderResult.reminderId}`);
    console.log(`   Agendado para: ${scheduledDate.toLocaleString("pt-BR")}`);

    // Listar lembretes pendentes
    console.log("\n📋 Listando lembretes pendentes...");
    const reminders = await client.reminders.list({
      status: ReminderStatus.Pending,
      page: 1,
      pageSize: 10,
    });

    console.log(`Total de lembretes pendentes: ${reminders.totalCount}`);
    reminders.items.forEach((reminder) => {
      console.log(`  - ${reminder.reason} (${new Date(reminder.scheduledFor).toLocaleString("pt-BR")})`);
    });

    console.log("\n✅ Processo concluído!");
  } catch (error: any) {
    console.error("❌ Erro:", error.message);
    if (error.statusCode) {
      console.error(`   Status: ${error.statusCode}`);
    }
  }
}

main().catch(console.error);

